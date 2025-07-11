using FlowSynx.Plugins.Google.Storage.Extensions;

namespace FlowSynx.Plugins.Google.Cloud.UnitTests.Extensions;

public class ByteExtensionsTests
{
    [Fact]
    public void ToHexString_WithNullInput_ReturnsEmptyString()
    {
        // Arrange
        byte[]? bytes = null;

        // Act
        var result = ByteExtensions.ToHexString(bytes);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToHexString_WithEmptyArray_ReturnsEmptyString()
    {
        // Arrange
        byte[] bytes = Array.Empty<byte>();

        // Act
        var result = ByteExtensions.ToHexString(bytes);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ToHexString_WithBytes_ReturnsCorrectHex()
    {
        // Arrange
        byte[] bytes = new byte[] { 0x0F, 0xA0, 0xB1 };

        // Act
        var result = ByteExtensions.ToHexString(bytes);

        // Assert
        Assert.Equal("0FA0B1", result);
    }
}