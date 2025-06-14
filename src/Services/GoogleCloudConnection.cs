using FlowSynx.Plugins.Google.Cloud.Models;
using Google.Cloud.Storage.V1;
using Newtonsoft.Json;

namespace FlowSynx.Plugins.Google.Cloud.Services;

internal class GoogleCloudConnection: IGoogleCloudConnection
{
    private readonly IStorageClientFactory _factory;
    private readonly IGoogleCredentialProvider _credentialProvider;

    public GoogleCloudConnection(IStorageClientFactory factory, IGoogleCredentialProvider credentialProvider)
    {
        _factory = factory;
        _credentialProvider = credentialProvider;
    }

    public StorageClient Connect(GoogleCloudSpecifications specifications)
    {
        var jsonObject = new
        {
            type = specifications.Type,
            project_id = specifications.ProjectId,
            private_key_id = specifications.PrivateKeyId,
            private_key = specifications.PrivateKey,
            client_email = specifications.ClientEmail,
            client_id = specifications.ClientId,
            auth_uri = specifications.AuthUri,
            token_uri = specifications.TokenUri,
            auth_provider_x509_cert_url = specifications.AuthProviderX509CertUrl,
            client_x509_cert_url = specifications.ClientX509CertUrl,
            universe_domain = specifications.UniverseDomain
        };

        var json = JsonConvert.SerializeObject(jsonObject);
        var credential = _credentialProvider.FromJson(json);
        return _factory.Create(credential);
    }
}