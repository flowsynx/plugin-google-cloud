using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace FlowSynx.Plugins.Google.Storage.Services;

public interface IStorageClientFactory
{
    StorageClient Create(GoogleCredential credential);
}
