using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Unit tests for RefreshTokenService
/// </summary>
public class RefreshTokenServiceTest : IDisposable
{
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly Mock<ILogger<RefreshTokenService>> _mockLogger;
    private readonly RefreshTokenService _refreshTokenService;

    public RefreshTokenServiceTest()
    {
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockLogger = new Mock<ILogger<RefreshTokenService>>();

        // Mock UserManager following the pattern from existing tests
        var mockUserStore = new Mock<IUserStore<IdentityUser>>();
        _mockUserManager = new Mock<UserManager<IdentityUser>>(
            mockUserStore.Object,
            Options.Create(new IdentityOptions()),
            new Mock<IPasswordHasher<IdentityUser>>().Object,
            Array.Empty<IUserValidator<IdentityUser>>(),
            Array.Empty<IPasswordValidator<IdentityUser>>(),
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<IdentityUser>>>().Object);

        _refreshTokenService = new RefreshTokenService(
            _mockRefreshTokenRepository.Object,
            _mockUserManager.Object,
            _mockLogger.Object);
    }

    #region GenerateRefreshTokenAsync Tests

    [Fact]
    public async Task GenerateRefreshTokenAsync_ReturnsBase64String()
    {
        // Act
        var result = await _refreshTokenService.GenerateRefreshTokenAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var bytes = Convert.FromBase64String(result);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ReturnsCorrectByteLength()
    {
        // Act
        var result = await _refreshTokenService.GenerateRefreshTokenAsync();

        // Assert
        var bytes = Convert.FromBase64String(result);
        Assert.Equal(TokenConstants.RefreshTokenLength, bytes.Length);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ReturnsDifferentValuesOnMultipleCalls()
    {
        // Act
        var result1 = await _refreshTokenService.GenerateRefreshTokenAsync();
        var result2 = await _refreshTokenService.GenerateRefreshTokenAsync();

        // Assert
        Assert.NotEqual(result1, result2);
    }

    #endregion

    #region HashRefreshToken Tests

    [Fact]
    public void HashRefreshToken_ReturnsBase64String()
    {
        // Arrange
        var token = "test-refresh-token";

        // Act
        var result = _refreshTokenService.HashRefreshToken(token);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var bytes = Convert.FromBase64String(result);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void HashRefreshToken_ReturnsDeterministicHash()
    {
        // Arrange
        var token = "test-refresh-token";

        // Act
        var hash1 = _refreshTokenService.HashRefreshToken(token);
        var hash2 = _refreshTokenService.HashRefreshToken(token);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashRefreshToken_ReturnsDifferentHashesForDifferentInputs()
    {
        // Arrange
        var token1 = "test-refresh-token-1";
        var token2 = "test-refresh-token-2";

        // Act
        var hash1 = _refreshTokenService.HashRefreshToken(token1);
        var hash2 = _refreshTokenService.HashRefreshToken(token2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashRefreshToken_ReturnsDifferentHashesForSimilarInputs()
    {
        // Arrange
        var token1 = "test-refresh-token";
        var token2 = "test-refresh-token!"; // One character difference

        // Act
        var hash1 = _refreshTokenService.HashRefreshToken(token1);
        var hash2 = _refreshTokenService.HashRefreshToken(token2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    #endregion

    #region CreateRefreshTokenAsync Tests

    [Fact]
    public async Task CreateRefreshTokenAsync_ReturnsEntityWithCorrectUserId()
    {
        // Arrange
        var userId = "user-123";
        _mockRefreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync((RefreshToken rt) => rt);
        _mockRefreshTokenRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var (entity, token) = await _refreshTokenService.CreateRefreshTokenAsync(userId);

        // Assert
        Assert.Equal(userId, entity.UserId);
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_ReturnsEntityWithHashedToken()
    {
        // Arrange
        var userId = "user-123";
        RefreshToken? capturedEntity = null;
        _mockRefreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(rt => capturedEntity = rt)
            .ReturnsAsync((RefreshToken rt) => rt);
        _mockRefreshTokenRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var (entity, token) = await _refreshTokenService.CreateRefreshTokenAsync(userId);

        // Assert
        Assert.NotNull(capturedEntity);
        var expectedHash = _refreshTokenService.HashRefreshToken(token);
        Assert.Equal(expectedHash, entity.TokenHash);
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_ReturnsEntityWithIsRevokedFalse()
    {
        // Arrange
        var userId = "user-123";
        _mockRefreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync((RefreshToken rt) => rt);
        _mockRefreshTokenRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var (entity, _) = await _refreshTokenService.CreateRefreshTokenAsync(userId);

        // Assert
        Assert.False(entity.IsRevoked);
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_ReturnsEntityWithExpiryAroundUtcNowPlus7Days()
    {
        // Arrange
        var userId = "user-123";
        _mockRefreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync((RefreshToken rt) => rt);
        _mockRefreshTokenRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var (entity, _) = await _refreshTokenService.CreateRefreshTokenAsync(userId);

        // Assert
        var expectedExpiry = DateTime.UtcNow.AddDays(TokenConstants.RefreshTokenExpirationDays);
        var tolerance = TimeSpan.FromSeconds(5);
        Assert.InRange(entity.ExpiresAt, expectedExpiry - tolerance, expectedExpiry + tolerance);
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_OnRepositoryFailure_ThrowsException()
    {
        // Arrange
        var userId = "user-123";
        _mockRefreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _refreshTokenService.CreateRefreshTokenAsync(userId));
    }

    #endregion

    #region ValidateRefreshTokenAsync Tests

    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenRevokedTokenReuse_ThrowsValidationException()
    {
        // Arrange
        var userId = "user-123";
        var existingRevokedToken = new RefreshToken
        {
            Id = 1,
            UserId = userId,
            TokenHash = "existing-hash",
            IsRevoked = true,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(existingRevokedToken);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _refreshTokenService.ValidateRefreshTokenAsync("some-token"));
    }

    #endregion

    #region RevokeRefreshTokenAsync Tests

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var userId = "user-123";
        var token = "valid-token";
        var storedToken = new RefreshToken
        {
            Id = 1,
            UserId = userId,
            TokenHash = "hash",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(storedToken);
        _mockRefreshTokenRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);
        _mockRefreshTokenRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _refreshTokenService.RevokeRefreshTokenAsync(token, userId);

        // Assert
        Assert.True(result);
        Assert.True(storedToken.IsRevoked);
        Assert.NotNull(storedToken.RevokedAt);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithNonExistentToken_ReturnsFalse()
    {
        // Arrange
        var userId = "user-123";
        var token = "non-existent-token";

        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _refreshTokenService.RevokeRefreshTokenAsync(token, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithWrongUser_ReturnsFalse()
    {
        // Arrange
        var userId = "user-123";
        var token = "valid-token";
        var storedToken = new RefreshToken
        {
            Id = 1,
            UserId = "different-user",
            TokenHash = "hash",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(storedToken);

        // Act
        var result = await _refreshTokenService.RevokeRefreshTokenAsync(token, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithRepositoryFailure_ReturnsFalse()
    {
        // Arrange
        var userId = "user-123";
        var token = "valid-token";
        var storedToken = new RefreshToken
        {
            Id = 1,
            UserId = userId,
            TokenHash = "hash",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(storedToken);
        _mockRefreshTokenRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _refreshTokenService.RevokeRefreshTokenAsync(token, userId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region RotateRefreshTokenAsync Tests

    [Fact]
    public async Task RotateRefreshTokenAsync_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        var userId = "user-123";
        var oldToken = "valid-old-token";
        var storedToken = new RefreshToken
        {
            Id = 1,
            UserId = userId,
            TokenHash = "old-hash",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        RefreshToken? capturedNewEntity = null;
        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(storedToken);
        _mockRefreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(rt => capturedNewEntity = rt)
            .ReturnsAsync((RefreshToken rt) => rt);
        _mockRefreshTokenRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);
        _mockRefreshTokenRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var (entity, newToken) = await _refreshTokenService.RotateRefreshTokenAsync(oldToken, userId);

        // Assert
        Assert.NotNull(entity);
        Assert.NotNull(newToken);
        Assert.NotEmpty(newToken);
        Assert.True(storedToken.IsRevoked);
        Assert.NotNull(storedToken.RevokedAt);
        Assert.NotNull(capturedNewEntity);
        Assert.Equal(capturedNewEntity.TokenHash, storedToken.ReplacedByTokenHash);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WithInvalidToken_PropagatesValidationException()
    {
        // Arrange
        var userId = "user-123";
        var oldToken = "invalid-token";

        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _refreshTokenService.RotateRefreshTokenAsync(oldToken, userId));
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WithCreateFailure_WrapsAsEntityServiceException()
    {
        // Arrange
        var userId = "user-123";
        var oldToken = "valid-old-token";
        var storedToken = new RefreshToken
        {
            Id = 1,
            UserId = userId,
            TokenHash = "old-hash",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(storedToken);
        _mockRefreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .ThrowsAsync(new InvalidOperationException("Create failed"));

        // Act & Assert
        await Assert.ThrowsAsync<EntityServiceException>(
            () => _refreshTokenService.RotateRefreshTokenAsync(oldToken, userId));
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WithUpdateFailure_WrapsAsEntityServiceException()
    {
        // Arrange
        var userId = "user-123";
        var oldToken = "valid-old-token";
        var storedToken = new RefreshToken
        {
            Id = 1,
            UserId = userId,
            TokenHash = "old-hash",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        var newTokenEntity = new RefreshToken
        {
            Id = 2,
            UserId = userId,
            TokenHash = "new-hash",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(storedToken);
        _mockRefreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync(newTokenEntity);
        _mockRefreshTokenRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>()))
            .ThrowsAsync(new InvalidOperationException("Update failed"));

        // Act & Assert
        await Assert.ThrowsAsync<EntityServiceException>(
            () => _refreshTokenService.RotateRefreshTokenAsync(oldToken, userId));
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WithSaveFailure_WrapsAsEntityServiceException()
    {
        // Arrange
        var userId = "user-123";
        var oldToken = "valid-old-token";
        var storedToken = new RefreshToken
        {
            Id = 1,
            UserId = userId,
            TokenHash = "old-hash",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        var newTokenEntity = new RefreshToken
        {
            Id = 2,
            UserId = userId,
            TokenHash = "new-hash",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(storedToken);
        _mockRefreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync(newTokenEntity);
        _mockRefreshTokenRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);
        _mockRefreshTokenRepository
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new InvalidOperationException("Save failed"));

        // Act & Assert
        await Assert.ThrowsAsync<EntityServiceException>(
            () => _refreshTokenService.RotateRefreshTokenAsync(oldToken, userId));
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_SetsReplacedByTokenHashOnOldToken()
    {
        // Arrange
        var userId = "user-123";
        var oldToken = "valid-old-token";
        var storedToken = new RefreshToken
        {
            Id = 1,
            UserId = userId,
            TokenHash = "old-hash",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        RefreshToken? capturedNewEntity = null;
        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(storedToken);
        _mockRefreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(rt => capturedNewEntity = rt)
            .ReturnsAsync((RefreshToken rt) => rt);
        _mockRefreshTokenRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);
        _mockRefreshTokenRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _refreshTokenService.RotateRefreshTokenAsync(oldToken, userId);

        // Assert
        Assert.NotNull(capturedNewEntity);
        Assert.Equal(capturedNewEntity.TokenHash, storedToken.ReplacedByTokenHash);
    }

    #endregion

    public void Dispose()
    {
        // Cleanup if needed
    }
}
