using FlowSynx.PluginCore;
using Google.Cloud.Storage.V1;
using System.Text;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace FlowSynx.Plugins.Google.Storage.Extensions;

static class ConverterExtensions
{
    public static async Task<PluginContext> ToContext(this StorageClient client, string bucketName, 
        string path, bool? includeMetadata, CancellationToken cancellationToken)
    {
        var ms = new MemoryStream();
        var response = await client.DownloadObjectAsync(bucketName,
            path, ms, cancellationToken: cancellationToken).ConfigureAwait(false);

        ms.Seek(0, SeekOrigin.Begin);

        var dataBytes = ms.ToArray();
        var isBinaryFile = IsBinaryFile(dataBytes);
        var rawData = isBinaryFile ? dataBytes : null;
        var content = !isBinaryFile ? Encoding.UTF8.GetString(dataBytes) : null;

        var fullPath = PathHelper.Combine(bucketName, path);
        var context = new PluginContext(fullPath, "File")
        {
            RawData = rawData,
            Content = content
        };

        if (includeMetadata is true)
        {
            AddProperties(context, response);
        }

        return context;
    }

    private static bool IsBinaryFile(byte[] data, int sampleSize = 1024)
    {
        if (data == null || data.Length == 0)
            return false;

        int checkLength = Math.Min(sampleSize, data.Length);
        int nonPrintableCount = data.Take(checkLength)
            .Count(b => (b < 8 || (b > 13 && b < 32)) && b != 9 && b != 10 && b != 13);

        double threshold = 0.1; // 10% threshold of non-printable characters
        return (double)nonPrintableCount / checkLength > threshold;
    }

    private static void AddProperties(PluginContext context, Object googleObject)
    {
        context.Metadata.Add("ContentType", googleObject.ContentType);
        context.Metadata.Add("CacheControl", googleObject.CacheControl);

        if (googleObject.ComponentCount.HasValue)
            context.Metadata.Add("ComponentControl", googleObject.ComponentCount);

        context.Metadata.Add("ContentDisposition", googleObject.ContentDisposition);
        context.Metadata.Add("ContentEncoding", googleObject.ContentEncoding);
        context.Metadata.Add("ContentLanguage", googleObject.ContentLanguage);
        context.Metadata.Add("ContentHash", Convert.FromBase64String(googleObject.Md5Hash).ToHexString());

        context.Metadata.Add("Crc32", googleObject.Crc32c);

        context.Metadata.Add("ETag", googleObject.ETag);

        if (googleObject.EventBasedHold.HasValue)
            context.Metadata.Add("EventBasedHold", googleObject.EventBasedHold);

        if (googleObject.Generation.HasValue)
            context.Metadata.Add("Generation", googleObject.Generation);

        context.Metadata.Add("Id", googleObject.Id);
        context.Metadata.Add("KmsKeyName", googleObject.KmsKeyName);
        context.Metadata.Add("MediaLink", googleObject.MediaLink);

        if (googleObject.Metageneration.HasValue)
            context.Metadata.Add("MetaGeneration", googleObject.Metageneration);

        context.Metadata.Add("Owner", googleObject.Owner);

        if (googleObject.RetentionExpirationTimeDateTimeOffset.HasValue)
            context.Metadata.Add("RetentionExpirationTime", googleObject.RetentionExpirationTimeDateTimeOffset);

        context.Metadata.Add("StorageClass", googleObject.StorageClass);

        if (googleObject.TemporaryHold.HasValue)
            context.Metadata.Add("TemporaryHold", googleObject.TemporaryHold);

        if (googleObject.TimeCreatedDateTimeOffset.HasValue)
            context.Metadata.Add("TimeCreated", googleObject.TimeCreatedDateTimeOffset);

        if (googleObject.TimeDeletedDateTimeOffset.HasValue)
            context.Metadata.Add("TimeDeleted", googleObject.TimeDeletedDateTimeOffset);

        if (googleObject.TimeStorageClassUpdatedDateTimeOffset.HasValue)
            context.Metadata.Add("TimeStorageClassUpdated", googleObject.TimeStorageClassUpdatedDateTimeOffset);
    }
}