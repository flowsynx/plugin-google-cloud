using Google.Apis.Auth.OAuth2;

namespace FlowSynx.Plugins.Google.Storage.Services;

public class GoogleCredentialProvider : IGoogleCredentialProvider
{
    public GoogleCredential FromJson(string json) => GoogleCredential.FromJson(json);
}