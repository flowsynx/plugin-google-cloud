using Google.Apis.Auth.OAuth2;

namespace FlowSynx.Plugins.Google.Storage.Services;

public interface IGoogleCredentialProvider
{
    GoogleCredential FromJson(string json);
}