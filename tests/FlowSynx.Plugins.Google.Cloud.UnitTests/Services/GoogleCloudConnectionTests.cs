using FlowSynx.Plugins.Google.Storage.Models;
using FlowSynx.Plugins.Google.Storage.Services;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Moq;

namespace FlowSynx.Plugins.Google.Storage.UnitTests.Services;

public class GoogleCloudConnectionTests
{
    [Fact]
    public void Connect_ShouldReturnStorageClient_WhenSpecificationsAreValid()
    {
        // Arrange
        var mockFactory = new Mock<IStorageClientFactory>();
        var mockClient = new Mock<StorageClient>();
        mockFactory.Setup(f => f.Create(It.IsAny<GoogleCredential>())).Returns(mockClient.Object);

        var fakeCredential = GoogleCredential.FromAccessToken("fake-token");
        var mockCredentialProvider = new Mock<IGoogleCredentialProvider>();
        mockCredentialProvider.Setup(p => p.FromJson(It.IsAny<string>()))
                              .Returns(fakeCredential);

        var connection = new GoogleCloudConnection(mockFactory.Object, mockCredentialProvider.Object);
        var specifications = new GoogleCloudSpecifications
        {
            ProjectId = "project",
            PrivateKeyId = "keyid",
            PrivateKey = "fake-key",
            ClientEmail = "test@example.com",
            ClientId = "id",
            BucketName = "test",
        };

        // Act
        var result = connection.Connect(specifications);

        // Assert
        Assert.Equal(mockClient.Object, result);
        mockFactory.Verify(f => f.Create(It.IsAny<GoogleCredential>()), Times.Once);
        Assert.NotNull(result);
    }
}
