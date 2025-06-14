using Google.Cloud.Storage.V1;
using Google;
using System.Net;
using System.Text;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.Google.Cloud.Models;
using FlowSynx.Plugins.Google.Cloud.Extensions;
using System.Text.RegularExpressions;

namespace FlowSynx.Plugins.Google.Cloud.Services;

internal class GoogleCloudManager : IGoogleCloudManager
{
    private readonly IPluginLogger _logger;
    private readonly StorageClient _client;
    private readonly string _bucketName;

    public GoogleCloudManager(IPluginLogger logger, StorageClient client, string bucketName)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(bucketName);
        _logger = logger;
        _client = client;
        _bucketName = bucketName;
    }

    public async Task Create(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var createParameters = parameters.ToObject<CreateParameters>();
        await CreateEntity(createParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task Delete(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var deleteParameter = parameters.ToObject<DeleteParameters>();
        await DeleteEntity(deleteParameter, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> Exist(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var existParameters = parameters.ToObject<ExistParameters>();
        return await ExistEntity(existParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<PluginContext>> List(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var listParameters = parameters.ToObject<ListParameters>();
        return await ListEntities(listParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task Purge(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var purgeParameters = parameters.ToObject<PurgeParameters>();
        await PurgeEntity(purgeParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PluginContext> Read(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var readParameters = parameters.ToObject<ReadParameters>();
        return await ReadEntity(readParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task Write(PluginParameters parameters, CancellationToken cancellationToken)
    {
        var writeParameters = parameters.ToObject<WriteParameters>();
        await WriteEntity(writeParameters, cancellationToken).ConfigureAwait(false);
    }

    #region internal methods
    private async Task CreateEntity(CreateParameters createParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(createParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new Exception(Resources.ThePathIsNotDirectory);

        var isExist = await BucketExists(_bucketName, cancellationToken);
        if (!isExist)
            throw new Exception(string.Format(Resources.BacketIsNotExist, _bucketName));
        
        if (!string.IsNullOrEmpty(path))
            await AddFolder(_bucketName, path, cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteEntity(DeleteParameters deleteParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(deleteParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            if (PathHelper.IsFile(path))
            {
                var isExist = await ObjectExists(_bucketName, path, cancellationToken);
                if (!isExist)
                {
                    _logger.LogWarning(string.Format(Resources.TheSpecifiedPathIsNotExist, path));
                    return;
                }

                await _client.DeleteObjectAsync(_bucketName, path, cancellationToken: cancellationToken).ConfigureAwait(false);
                _logger.LogInfo(string.Format(Resources.TheSpecifiedPathWasDeleted, path));
                return;
            }

            await DeleteAll(_bucketName, path, cancellationToken: cancellationToken);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task<bool> ExistEntity(ExistParameters existParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(existParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        try
        {
            if (PathHelper.IsFile(path))
                return await ObjectExists(_bucketName, path, cancellationToken);

            if (PathHelper.IsRootPath(path))
                return await BucketExists(_bucketName, cancellationToken);

            return await FolderExist(_bucketName, path, cancellationToken);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task WriteEntity(WriteParameters writeParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(writeParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        var dataValue = writeParameters.Data;
        var pluginContextes = new List<PluginContext>();

        if (dataValue is PluginContext pluginContext)
        {
            if (!PathHelper.IsFile(path))
                throw new Exception(Resources.ThePathIsNotFile);

            pluginContextes.Add(pluginContext);
        }
        else if (dataValue is IEnumerable<PluginContext> pluginContextesList)
        {
            if (!PathHelper.IsDirectory(path))
                throw new Exception(Resources.ThePathIsNotDirectory);

            pluginContextes.AddRange(pluginContextesList);
        }
        else if (dataValue is string data)
        {
            if (!PathHelper.IsFile(path))
                throw new Exception(Resources.ThePathIsNotFile);

            var context = CreateContextFromStringData(path, data);
            pluginContextes.Add(context);
        }
        else
        {
            throw new NotSupportedException("The entered data format is not supported!");
        }

        foreach (var context in pluginContextes)
        {
            await WriteEntityFromContext(path, context, writeParameters.Overwrite, cancellationToken).ConfigureAwait(false);
        }
    }

    private PluginContext CreateContextFromStringData(string path, string data)
    {
        var root = Path.GetPathRoot(path) ?? string.Empty;
        var relativePath = Path.GetRelativePath(root, path);
        var dataBytesArray = data.IsBase64String() ? data.Base64ToByteArray() : data.ToByteArray();

        return new PluginContext(relativePath, "File")
        {
            RawData = dataBytesArray,
        };
    }

    private async Task WriteEntityFromContext(string path, PluginContext context, bool overwrite,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] dataToWrite;

        if (context.RawData is not null)
            dataToWrite = context.RawData;
        else if (context.Content is not null)
            dataToWrite = Encoding.UTF8.GetBytes(context.Content);
        else
            throw new InvalidDataException($"The entered data is invalid for '{context.Id}'");

        var rootPath = Path.GetPathRoot(context.Id);
        string relativePath = context.Id;

        if (!string.IsNullOrEmpty(rootPath))
            relativePath = Path.GetRelativePath(rootPath, context.Id);

        var fullPath = PathHelper.IsDirectory(path) ? PathHelper.Combine(path, relativePath) : path;

        if (!PathHelper.IsFile(fullPath))
            throw new Exception(Resources.ThePathIsNotFile);

        var isExist = await ObjectExists(_bucketName, fullPath, cancellationToken);
        if (isExist && overwrite is false)
            throw new Exception(string.Format(Resources.FileIsAlreadyExistAndCannotBeOverwritten, fullPath));

        using var stream = new MemoryStream(dataToWrite);

        try
        {
            await _client.UploadObjectAsync(_bucketName, path,
                null, stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task<PluginContext> ReadEntity(ReadParameters readParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(readParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsFile(path))
            throw new Exception(Resources.ThePathIsNotFile);

        try
        {
            var isExist = await ObjectExists(_bucketName, path, cancellationToken);

            if (!isExist)
                throw new Exception(string.Format(Resources.TheSpecifiedPathIsNotExist, path));

            return await _client.ToContext(_bucketName, path, true, cancellationToken).ConfigureAwait(false);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception(string.Format(Resources.ResourceNotExist, path));
        }
    }

    private async Task PurgeEntity(PurgeParameters purgeParameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(purgeParameters.Path);
        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        await DeleteAll(_bucketName, path, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IEnumerable<PluginContext>> ListEntities(ListParameters listParameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = PathHelper.ToUnixPath(listParameters.Path);

        if (string.IsNullOrEmpty(path))
            throw new Exception(Resources.TheSpecifiedPathMustBeNotEmpty);

        if (!PathHelper.IsDirectory(path))
            throw new Exception(Resources.ThePathIsNotDirectory);

        return await ListObjects(_bucketName, listParameters, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IEnumerable<PluginContext>> ListObjects(
        string bucketName, 
        ListParameters listParameters, 
        CancellationToken cancellationToken)
    {
        int count = 0;
        var result = new List<PluginContext>();

        Regex? regex = null;
        if (!string.IsNullOrEmpty(listParameters.Filter))
        {
            var regexOptions = listParameters.CaseSensitive is true ? RegexOptions.IgnoreCase : RegexOptions.None;
            regex = new Regex(listParameters.Filter, regexOptions);
        }

        var request = _client.Service.Objects.List(bucketName);
        request.Prefix = FormatFolderPrefix(listParameters.Path);
        request.Delimiter = listParameters.Recurse is true ? null : PathHelper.PathSeparatorString;

        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            var serviceObjects = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (serviceObjects.Items != null)
            {
                foreach (var item in serviceObjects.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item == null)
                        continue;

                    if (item.Name.EndsWith(PathHelper.PathSeparator))
                        continue;

                    if (count >= listParameters.MaxResults)
                        break;

                    var isMatched = regex != null && regex.IsMatch(item.Name);
                    if (listParameters.Filter == null || isMatched)
                    {
                        var context = await _client.ToContext(item.Bucket, item.Name, listParameters.IncludeMetadata, cancellationToken);
                        result.Add(context);
                        count++;
                    }
                }

                if (count >= listParameters.MaxResults) break;
            }

            request.PageToken = serviceObjects.NextPageToken;
        }
        while (request.PageToken != null);

        return result;
    }

    private async Task AddFolder(string bucketName, string folderName, CancellationToken cancellationToken)
    {
        if (!folderName.EndsWith(PathHelper.PathSeparator))
            folderName += PathHelper.PathSeparator;

        var content = Encoding.UTF8.GetBytes("");
        await _client.UploadObjectAsync(bucketName, folderName, "application/x-directory",
            new MemoryStream(content), cancellationToken: cancellationToken);
        _logger.LogInfo($"Folder '{folderName}' was created successfully.");
    }

    private async Task<bool> FolderExist(string bucketName, string path, CancellationToken cancellationToken)
    {
        var folderPrefix = path + PathHelper.PathSeparator;
        var request = _client.Service.Objects.List(bucketName);
        request.Prefix = folderPrefix;
        request.Delimiter = PathHelper.PathSeparatorString;

        var serviceObjects = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        if (serviceObjects == null)
            return false;

        if (serviceObjects.Items is { Count: > 0 })
            return serviceObjects.Items?.Any(x => x.Name.StartsWith(folderPrefix)) ?? false;

        return serviceObjects.Prefixes?.Any(x => x.StartsWith(folderPrefix)) ?? false;
    }

    private async Task DeleteAll(string bucketName, string folderName, CancellationToken cancellationToken)
    {
        var request = _client.Service.Objects.List(bucketName);
        request.Prefix = folderName;
        request.Delimiter = null;

        do
        {
            var serviceObjects = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            if (serviceObjects.Items != null)
            {
                foreach (var item in serviceObjects.Items)
                {
                    if (item == null)
                        continue;

                    await _client.DeleteObjectAsync(bucketName, item.Name,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            request.PageToken = serviceObjects.NextPageToken;
        }
        while (request.PageToken != null);
    }

    private async Task<bool> BucketExists(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            await _client.GetBucketAsync(bucketName, null, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task<bool> ObjectExists(string bucketName, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            await _client.GetObjectAsync(bucketName, fileName, null, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private string? FormatFolderPrefix(string folderPath)
    {
        folderPath = PathHelper.Normalize(folderPath);

        if (PathHelper.IsRootPath(folderPath))
            return null;

        if (!folderPath.EndsWith(PathHelper.PathSeparator))
            folderPath += PathHelper.PathSeparator;

        return folderPath;
    }
    #endregion
}
