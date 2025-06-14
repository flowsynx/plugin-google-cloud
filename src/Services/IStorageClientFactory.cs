using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;

namespace FlowSynx.Plugins.Google.Cloud.Services;

public interface IStorageClientFactory
{
    StorageClient Create(GoogleCredential credential);
}
