using FlowSynx.Plugins.Google.Cloud.Models;
using FlowSynx.Plugins.Google.Cloud.Services;
using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Helpers;
using FlowSynx.PluginCore.Extensions;

namespace FlowSynx.Plugins.Google.Cloud;

public class GoogleCloudPlugin : IPlugin
{
    private IGoogleCloudManager _manager = null!;
    private GoogleCloudSpecifications? _googleCloudSpecifications;
    private bool _isInitialized;

    public PluginMetadata Metadata
    {
        get
        {
            return new PluginMetadata
            {
                Id = Guid.Parse("d3c52770-f001-4ea3-93b7-f113a956a091"),
                Name = "Google.Cloud",
                CompanyName = "FlowSynx",
                Description = Resources.PluginDescription,
                Version = new PluginVersion(1, 0, 0),
                Namespace = PluginNamespace.Connectors,
                Authors = new List<string> { "FlowSynx" },
                Copyright = "© FlowSynx. All rights reserved.",
                Icon = "flowsynx.png",
                ReadMe = "README.md",
                RepositoryUrl = "https://github.com/flowsynx/plugin-google-cloud",
                ProjectUrl = "https://flowsynx.io",
                Tags = new List<string>() { "FlowSynx", "Google", "Storage", "Cloud" }
            };
        }
    }

    public PluginSpecifications? Specifications { get; set; }
    public Type SpecificationsType => typeof(GoogleCloudSpecifications);

    public Task Initialize(IPluginLogger logger)
    {
        if (ReflectionHelper.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        ArgumentNullException.ThrowIfNull(logger);
        var connection = new GoogleCloudConnection();
        _googleCloudSpecifications = Specifications.ToObject<GoogleCloudSpecifications>();
        var client = connection.Connect(_googleCloudSpecifications);
        _manager = new GoogleCloudManager(logger, client, _googleCloudSpecifications.BucketName);
        _isInitialized = true;
        return Task.CompletedTask;
    }

    public Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken)
    {
        if (ReflectionHelper.IsCalledViaReflection())
            throw new InvalidOperationException(Resources.ReflectionBasedAccessIsNotAllowed);

        if (!_isInitialized)
            throw new InvalidOperationException($"Plugin '{Metadata.Name}' v{Metadata.Version} is not initialized.");

        var operationParameter = parameters.ToObject<OperationParameter>();
        var operation = operationParameter.Operation;

        if (OperationMap.TryGetValue(operation, out var handler))
            return handler(parameters, cancellationToken);

        throw new NotSupportedException($"Google Cloud plugin: Operation '{operation}' is not supported.");
    }

    private Dictionary<string, Func<PluginParameters, CancellationToken, Task<object?>>> OperationMap => new(StringComparer.OrdinalIgnoreCase)
    {
        ["create"] = async (parameters, cancellationToken) => { await _manager.Create(parameters, cancellationToken); return null; },
        ["delete"] = async (parameters, cancellationToken) => { await _manager.Delete(parameters, cancellationToken); return null; },
        ["exist"] = async (parameters, cancellationToken) => await _manager.Exist(parameters, cancellationToken),
        ["list"] = async (parameters, cancellationToken) => await _manager.List(parameters, cancellationToken),
        ["purge"] = async (parameters, cancellationToken) => { await _manager.Purge(parameters, cancellationToken); return null; },
        ["read"] = async (parameters, cancellationToken) => await _manager.Read(parameters, cancellationToken),
        ["write"] = async (parameters, cancellationToken) => { await _manager.Write(parameters, cancellationToken); return null; },
    };

    public IReadOnlyCollection<string> SupportedOperations => OperationMap.Keys;
}