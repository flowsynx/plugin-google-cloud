using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace FlowSynx.Plugins.Google.Cloud.Services;

internal class StorageClientFactory : IStorageClientFactory
{
    public StorageClient Create(GoogleCredential credential)
        => StorageClient.Create(credential);
}