using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using Moq;
using Xunit;

namespace attendance.testproject.IRepositories_Testing;

/// <summary>
/// Tests for IRefreshTokenRepository implementation
/// </summary>
public class RefreshTokenRepositoryTest
{
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;

    public RefreshTokenRepositoryTest()
    {
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
    }

    [Fact]
    public async Task GetByTokenHashAsync_ReturnsRefreshToken_WhenTokenExists()
    {
        // Arrange
        var tokenHash = "abc123hash";
        var expectedToken = new RefreshToken
        {
            Id = 1,
            TokenHash = tokenHash,
            UserId = "user123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(tokenHash))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _mockRefreshTokenRepository.Object.GetByTokenHashAsync(tokenHash);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tokenHash, result.TokenHash);
        Assert.Equal("user123", result.UserId);
        Assert.False(result.IsRevoked);

        _mockRefreshTokenRepository.Verify(r => r.GetByTokenHashAsync(tokenHash), Times.Once);
    }

    [Fact]
    public async Task GetByTokenHashAsync_ReturnsNull_WhenTokenDoesNotExist()
    {
        // Arrange
        var tokenHash = "nonexistent";
        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(tokenHash))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _mockRefreshTokenRepository.Object.GetByTokenHashAsync(tokenHash);

        // Assert
        Assert.Null(result);

        _mockRefreshTokenRepository.Verify(r => r.GetByTokenHashAsync(tokenHash), Times.Once);
    }

    [Fact]
    public async Task GetByReplacedTokenHashAsync_ReturnsRefreshToken_WhenReplacedTokenExists()
    {
        // Arrange
        var replacedTokenHash = "oldtoken456";
        var expectedToken = new RefreshToken
        {
            Id = 2,
            TokenHash = "newtoken789",
            ReplacedByTokenHash = replacedTokenHash,
            UserId = "user456",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = true
        };
        _mockRefreshTokenRepository
            .Setup(r => r.GetByReplacedTokenHashAsync(replacedTokenHash))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _mockRefreshTokenRepository.Object.GetByReplacedTokenHashAsync(replacedTokenHash);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(replacedTokenHash, result.ReplacedByTokenHash);
        Assert.Equal("user456", result.UserId);
        Assert.True(result.IsRevoked);

        _mockRefreshTokenRepository.Verify(r => r.GetByReplacedTokenHashAsync(replacedTokenHash), Times.Once);
    }

    [Fact]
    public async Task GetByReplacedTokenHashAsync_ReturnsNull_WhenReplacedTokenDoesNotExist()
    {
        // Arrange
        var replacedTokenHash = "nonexistent";
        _mockRefreshTokenRepository
            .Setup(r => r.GetByReplacedTokenHashAsync(replacedTokenHash))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _mockRefreshTokenRepository.Object.GetByReplacedTokenHashAsync(replacedTokenHash);

        // Assert
        Assert.Null(result);

        _mockRefreshTokenRepository.Verify(r => r.GetByReplacedTokenHashAsync(replacedTokenHash), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedRefreshToken()
    {
        // Arrange
        var newToken = new RefreshToken
        {
            TokenHash = "newtoken123",
            UserId = "user789",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
        var createdToken = new RefreshToken
        {
            Id = 3,
            TokenHash = "newtoken123",
            UserId = "user789",
            ExpiresAt = newToken.ExpiresAt,
            CreatedAt = newToken.CreatedAt,
            IsRevoked = false
        };
        _mockRefreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync(createdToken);

        // Act
        var result = await _mockRefreshTokenRepository.Object.CreateAsync(newToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
        Assert.Equal("newtoken123", result.TokenHash);
        Assert.Equal("user789", result.UserId);
        Assert.False(result.IsRevoked);

        _mockRefreshTokenRepository.Verify(r => r.CreateAsync(It.IsAny<RefreshToken>()), Times.Once);
    }


    [Fact]
    public async Task UpdateAsync_WithRevokedToken_UpdatesSuccessfully()
    {
        // Arrange
        var revokedToken = new RefreshToken
        {
            Id = 1,
            TokenHash = "token123",
            UserId = "user123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow
        };
        _mockRefreshTokenRepository
            .Setup(r => r.UpdateAsync(It.Is<RefreshToken>(t => t.IsRevoked)))
            .Returns(Task.CompletedTask);

        // Act
        await _mockRefreshTokenRepository.Object.UpdateAsync(revokedToken);

        // Assert
        _mockRefreshTokenRepository.Verify(r => r.UpdateAsync(It.Is<RefreshToken>(t => t.IsRevoked)), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenTokenExists()
    {
        // Arrange
        var tokenHash = "existingtoken";
        _mockRefreshTokenRepository
            .Setup(r => r.ExistsAsync(tokenHash))
            .ReturnsAsync(true);

        // Act
        var result = await _mockRefreshTokenRepository.Object.ExistsAsync(tokenHash);

        // Assert
        Assert.True(result);

        _mockRefreshTokenRepository.Verify(r => r.ExistsAsync(tokenHash), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenTokenDoesNotExist()
    {
        // Arrange
        var tokenHash = "nonexistenttoken";
        _mockRefreshTokenRepository
            .Setup(r => r.ExistsAsync(tokenHash))
            .ReturnsAsync(false);

        // Act
        var result = await _mockRefreshTokenRepository.Object.ExistsAsync(tokenHash);

        // Assert
        Assert.False(result);

        _mockRefreshTokenRepository.Verify(r => r.ExistsAsync(tokenHash), Times.Once);
    }

    [Fact]
    public async Task GetByTokenHashAsync_WithExpiredToken_ReturnsToken()
    {
        // Arrange
        var tokenHash = "expiredtoken";
        var expiredToken = new RefreshToken
        {
            Id = 4,
            TokenHash = tokenHash,
            UserId = "user999",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            IsRevoked = false
        };
        _mockRefreshTokenRepository
            .Setup(r => r.GetByTokenHashAsync(tokenHash))
            .ReturnsAsync(expiredToken);

        // Act
        var result = await _mockRefreshTokenRepository.Object.GetByTokenHashAsync(tokenHash);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ExpiresAt < DateTime.UtcNow);
        Assert.Equal(tokenHash, result.TokenHash);

        _mockRefreshTokenRepository.Verify(r => r.GetByTokenHashAsync(tokenHash), Times.Once);
    }
}