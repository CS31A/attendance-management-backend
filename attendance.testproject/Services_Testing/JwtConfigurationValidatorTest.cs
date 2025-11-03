using attendance_monitoring.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Unit tests for JwtConfigurationValidator
/// </summary>
public class JwtConfigurationValidatorTest
{
    [Fact]
    public void ValidateJwtConfiguration_WithValidConfig_ShouldNotThrow()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:Token"] = "ThisIsAValidTokenThatIsAtLeast32BytesLongForSecurityPurposes",
            ["AppSettings:Issuer"] = "TestIssuer",
            ["AppSettings:Audience"] = "TestAudience"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert
        var exception = Record.Exception(() => JwtConfigurationValidator.ValidateJwtConfiguration(configuration));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateJwtConfiguration_WithMissingToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:Issuer"] = "TestIssuer",
            ["AppSettings:Audience"] = "TestAudience"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => JwtConfigurationValidator.ValidateJwtConfiguration(configuration));
        Assert.Contains("JWT Token is missing or empty", exception.Message);
    }

    [Fact]
    public void ValidateJwtConfiguration_WithShortToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:Token"] = "short", // Only 5 bytes, less than required 32 bytes
            ["AppSettings:Issuer"] = "TestIssuer",
            ["AppSettings:Audience"] = "TestAudience"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => JwtConfigurationValidator.ValidateJwtConfiguration(configuration));
        Assert.Contains("JWT Token is too short", exception.Message);
        Assert.Contains("Minimum length is 32 bytes", exception.Message);
    }

    [Fact]
    public void ValidateJwtConfiguration_WithMissingIssuer_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:Token"] = "ThisIsAValidTokenThatIsAtLeast32BytesLongForSecurityPurposes",
            ["AppSettings:Audience"] = "TestAudience"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => JwtConfigurationValidator.ValidateJwtConfiguration(configuration));
        Assert.Contains("JWT Issuer is missing or empty", exception.Message);
    }

    [Fact]
    public void ValidateJwtConfiguration_WithMissingAudience_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:Token"] = "ThisIsAValidTokenThatIsAtLeast32BytesLongForSecurityPurposes",
            ["AppSettings:Issuer"] = "TestIssuer"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => JwtConfigurationValidator.ValidateJwtConfiguration(configuration));
        Assert.Contains("JWT Audience is missing or empty", exception.Message);
    }

    [Fact]
    public void ValidateJwtConfiguration_WithMultipleErrors_ShouldThrowWithAllErrors()
    {
        // Arrange
        var configData = new Dictionary<string, string>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => JwtConfigurationValidator.ValidateJwtConfiguration(configuration));
        Assert.Contains("JWT Token is missing or empty", exception.Message);
        Assert.Contains("JWT Issuer is missing or empty", exception.Message);
        Assert.Contains("JWT Audience is missing or empty", exception.Message);
    }

    [Fact]
    public void GetValidatedToken_WithValidToken_ShouldReturnToken()
    {
        // Arrange
        var expectedToken = "ThisIsAValidTokenThatIsAtLeast32BytesLongForSecurityPurposes";
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:Token"] = expectedToken
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var token = JwtConfigurationValidator.GetValidatedToken(configuration);

        // Assert
        Assert.Equal(expectedToken, token);
    }

    [Fact]
    public void GetValidatedToken_WithShortToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:Token"] = "short"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => JwtConfigurationValidator.GetValidatedToken(configuration));
        Assert.Contains("JWT Token is too short", exception.Message);
    }

    [Fact]
    public void GetValidatedIssuer_WithValidIssuer_ShouldReturnIssuer()
    {
        // Arrange
        var expectedIssuer = "TestIssuer";
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:Issuer"] = expectedIssuer
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var issuer = JwtConfigurationValidator.GetValidatedIssuer(configuration);

        // Assert
        Assert.Equal(expectedIssuer, issuer);
    }

    [Fact]
    public void GetValidatedAudience_WithValidAudience_ShouldReturnAudience()
    {
        // Arrange
        var expectedAudience = "TestAudience";
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:Audience"] = expectedAudience
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act
        var audience = JwtConfigurationValidator.GetValidatedAudience(configuration);

        // Assert
        Assert.Equal(expectedAudience, audience);
    }

    [Fact]
    public void ValidateJwtConfiguration_WithExactly32ByteToken_ShouldNotThrow()
    {
        // Arrange - Create a token that is exactly 32 bytes
        var token32Bytes = "12345678901234567890123456789012"; // Exactly 32 ASCII characters = 32 bytes
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:Token"] = token32Bytes,
            ["AppSettings:Issuer"] = "TestIssuer",
            ["AppSettings:Audience"] = "TestAudience"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert
        var exception = Record.Exception(() => JwtConfigurationValidator.ValidateJwtConfiguration(configuration));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateJwtConfiguration_With31ByteToken_ShouldThrow()
    {
        // Arrange - Create a token that is exactly 31 bytes (one byte short)
        var token31Bytes = "1234567890123456789012345678901"; // 31 ASCII characters = 31 bytes
        var configData = new Dictionary<string, string>
        {
            ["AppSettings:Token"] = token31Bytes,
            ["AppSettings:Issuer"] = "TestIssuer",
            ["AppSettings:Audience"] = "TestAudience"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => JwtConfigurationValidator.ValidateJwtConfiguration(configuration));
        Assert.Contains("JWT Token is too short", exception.Message);
        Assert.Contains("31 bytes", exception.Message);
    }
}
