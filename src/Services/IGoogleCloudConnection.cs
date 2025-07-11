using FlowSynx.Plugins.Google.Storage.Models;
using Google.Cloud.Storage.V1;

namespace FlowSynx.Plugins.Google.Storage.Services;

public interface IGoogleCloudConnection
{
    StorageClient Connect(GoogleCloudSpecifications specifications);
}