using FlowSynx.Plugins.Google.Cloud.Models;
using Google.Cloud.Storage.V1;

namespace FlowSynx.Plugins.Google.Cloud.Services;

public interface IGoogleCloudConnection
{
    StorageClient Connect(GoogleCloudSpecifications specifications);
}