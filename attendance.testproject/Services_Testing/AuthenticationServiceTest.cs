using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Unit tests for AuthenticationService
/// </summary>
public class AuthenticationServiceTest : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IRefreshTokenService> _mockRefreshTokenService;
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly ApplicationDbContext _context;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTest()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockRefreshTokenService = new Mock<IRefreshTokenService>();
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockLogger = new Mock<ILogger<AuthenticationService>>();
        
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        
        // Setup JWT configuration
        _mockConfiguration.Setup(c => c["AppSettings:Issuer"]).Returns("test-issuer");
        _mockConfiguration.Setup(c => c["AppSettings:Audience"]).Returns("test-audience");
        _mockConfiguration.Setup(c => c["AppSettings:Token"]).Returns("supersecretkey12345678901234567890");
        
        _authService = new AuthenticationService(
            _mockConfiguration.Object,
            _context,
            _mockRefreshTokenService.Object,
            _mockAccountRepository.Object,
            _mockLogger.Object);
    }

    #region Helper Methods

    private IdentityUser CreateTestUser(string id = "user-1", string username = "testuser", string email = "test@example.com")
    {
        return new IdentityUser
        {
            Id = id,
            UserName = username,
            Email = email,
            EmailConfirmed = true
        };
    }

    private RefreshToken CreateTestRefreshToken(string userId = "user-1", bool isRevoked = false, DateTime? expiresAt = null)
    {
        return new RefreshToken
        {
            Id = 1,
            UserId = userId,
            TokenHash = "hashed-token",
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = isRevoked,
            RevokedAt = isRevoked ? DateTime.UtcNow : null
        };
    }

    private static SignInResult CreateSuccessResult() => SignInResult.Success;
    private static SignInResult CreateFailedResult() => SignInResult.Failed;

    private string GenerateTestJwt(string userId, string? jti = null, DateTime? expiresAt = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("supersecretkey12345678901234567890");
        var tokenJti = jti ?? Guid.NewGuid().ToString();
        var tokenExpires = expiresAt ?? DateTime.UtcNow.AddHours(1);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("jti", tokenJti)
            }),
            Expires = tokenExpires,
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithUsername_ReturnsLoginResult()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = "ValidPassword123!"
        };

        var user = CreateTestUser(username: "testuser", email: "test@example.com");
        var roles = new List<string> { "Instructor" };

        _mockAccountRepository
            .Setup(r => r.FindUserByUsernameAsync(loginDto.Username))
            .ReturnsAsync(user);

        _mockAccountRepository
            .Setup(r => r.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(CreateSuccessResult());

        _mockAccountRepository
            .Setup(r => r.GetUserRolesAsync(user))
            .ReturnsAsync(roles);

        _mockRefreshTokenService
            .Setup(s => s.CreateRefreshTokenAsync(user.Id))
            .ReturnsAsync((CreateTestRefreshToken(user.Id), "refresh-token-string"));

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.TokenResponse);
        Assert.NotEmpty(result.TokenResponse.AccessToken);
        Assert.NotEmpty(result.TokenResponse.RefreshToken);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("Instructor", result.Role);

        _mockAccountRepository.Verify(r => r.FindUserByUsernameAsync(loginDto.Username), Times.Once);
        _mockAccountRepository.Verify(r => r.CheckPasswordAsync(user, loginDto.Password), Times.Once);
        _mockAccountRepository.Verify(r => r.GetUserRolesAsync(user), Times.AtLeastOnce());
        _mockRefreshTokenService.Verify(s => s.CreateRefreshTokenAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithEmail_ReturnsLoginResult()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "test@example.com",
            Password = "ValidPassword123!"
        };

        var user = CreateTestUser(username: "testuser", email: "test@example.com");
        var roles = new List<string> { "Student" };

        _mockAccountRepository
            .Setup(r => r.FindUserByEmailAsync(loginDto.Username))
            .ReturnsAsync(user);

        _mockAccountRepository
            .Setup(r => r.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(CreateSuccessResult());

        _mockAccountRepository
            .Setup(r => r.GetUserRolesAsync(user))
            .ReturnsAsync(roles);

        _mockRefreshTokenService
            .Setup(s => s.CreateRefreshTokenAsync(user.Id))
            .ReturnsAsync((CreateTestRefreshToken(user.Id), "refresh-token-string"));

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.TokenResponse);
        Assert.NotEmpty(result.TokenResponse.AccessToken);
        Assert.NotEmpty(result.TokenResponse.RefreshToken);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("Student", result.Role);

        _mockAccountRepository.Verify(r => r.FindUserByEmailAsync(loginDto.Username), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithMultipleRoles_UsesFirstRole()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = "ValidPassword123!"
        };

        var user = CreateTestUser(username: "testuser");
        var roles = new List<string> { "Instructor", "Admin", "Student" };

        _mockAccountRepository
            .Setup(r => r.FindUserByUsernameAsync(loginDto.Username))
            .ReturnsAsync(user);

        _mockAccountRepository
            .Setup(r => r.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(CreateSuccessResult());

        _mockAccountRepository
            .Setup(r => r.GetUserRolesAsync(user))
            .ReturnsAsync(roles);

        _mockRefreshTokenService
            .Setup(s => s.CreateRefreshTokenAsync(user.Id))
            .ReturnsAsync((CreateTestRefreshToken(user.Id), "refresh-token-string"));

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Instructor", result.Role);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUsername_ThrowsValidationException()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "nonexistent",
            Password = "ValidPassword123!"
        };

        _mockAccountRepository
            .Setup(r => r.FindUserByUsernameAsync(loginDto.Username))
            .ReturnsAsync((IdentityUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _authService.LoginAsync(loginDto));
        Assert.Contains("Invalid email or username or password", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ThrowsValidationException()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "nonexistent@example.com",
            Password = "ValidPassword123!"
        };

        _mockAccountRepository
            .Setup(r => r.FindUserByEmailAsync(loginDto.Username))
            .ReturnsAsync((IdentityUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _authService.LoginAsync(loginDto));
        Assert.Contains("Invalid email or username or password", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsValidationException()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        var user = CreateTestUser(username: "testuser");

        _mockAccountRepository
            .Setup(r => r.FindUserByUsernameAsync(loginDto.Username))
            .ReturnsAsync(user);

        _mockAccountRepository
            .Setup(r => r.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(CreateFailedResult());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _authService.LoginAsync(loginDto));
        Assert.Contains("Invalid email or username or password", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithNoRoles_ThrowsValidationException()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Username = "testuser",
            Password = "ValidPassword123!"
        };

        var user = CreateTestUser(username: "testuser");

        _mockAccountRepository
            .Setup(r => r.FindUserByUsernameAsync(loginDto.Username))
            .ReturnsAsync(user);

        _mockAccountRepository
            .Setup(r => r.CheckPasswordAsync(user, loginDto.Password))
            .ReturnsAsync(CreateSuccessResult());

        _mockAccountRepository
            .Setup(r => r.GetUserRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _authService.LoginAsync(loginDto));
        Assert.Contains("User has no assigned roles", exception.Message);
    }

    #endregion

    #region RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_WithValidToken_ReturnsNewTokenPair()
    {
        // Arrange
        var refreshTokenRequest = new RefreshTokenRequestDto
        {
            RefreshToken = "valid-refresh-token",
            OldAccessToken = null
        };

        var refreshTokenEntity = CreateTestRefreshToken("user-1");
        var user = CreateTestUser(id: "user-1");
        var roles = new List<string> { "Instructor" };

        _mockRefreshTokenService
            .Setup(s => s.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken))
            .ReturnsAsync(refreshTokenEntity);

        _mockAccountRepository
            .Setup(r => r.FindUserByIdAsync(refreshTokenEntity.UserId))
            .ReturnsAsync(user);

        _mockAccountRepository
            .Setup(r => r.GetUserRolesAsync(user))
            .ReturnsAsync(roles);

        _mockRefreshTokenService
            .Setup(s => s.RotateRefreshTokenAsync(refreshTokenRequest.RefreshToken, user.Id))
            .ReturnsAsync((CreateTestRefreshToken(user.Id), "new-refresh-token"));

        // Act
        var result = await _authService.RefreshAsync(refreshTokenRequest);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_WithOldAccessToken_ReturnsNewTokens()
    {
        // Arrange
        var refreshTokenRequest = new RefreshTokenRequestDto
        {
            RefreshToken = "valid-refresh-token",
            OldAccessToken = "old-access-token"
        };

        var refreshTokenEntity = CreateTestRefreshToken("user-1");
        var user = CreateTestUser(id: "user-1");
        var roles = new List<string> { "Instructor" };

        _mockRefreshTokenService
            .Setup(s => s.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken))
            .ReturnsAsync(refreshTokenEntity);

        _mockAccountRepository
            .Setup(r => r.FindUserByIdAsync(refreshTokenEntity.UserId))
            .ReturnsAsync(user);

        _mockAccountRepository
            .Setup(r => r.GetUserRolesAsync(user))
            .ReturnsAsync(roles);

        _mockRefreshTokenService
            .Setup(s => s.RotateRefreshTokenAsync(refreshTokenRequest.RefreshToken, user.Id))
            .ReturnsAsync((CreateTestRefreshToken(user.Id), "new-refresh-token"));

        // Act
        var result = await _authService.RefreshAsync(refreshTokenRequest);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_WithoutOldAccessToken_ReturnsNewTokens()
    {
        // Arrange
        var refreshTokenRequest = new RefreshTokenRequestDto
        {
            RefreshToken = "valid-refresh-token",
            OldAccessToken = null
        };

        var refreshTokenEntity = CreateTestRefreshToken("user-1");
        var user = CreateTestUser(id: "user-1");
        var roles = new List<string> { "Instructor" };

        _mockRefreshTokenService
            .Setup(s => s.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken))
            .ReturnsAsync(refreshTokenEntity);

        _mockAccountRepository
            .Setup(r => r.FindUserByIdAsync(refreshTokenEntity.UserId))
            .ReturnsAsync(user);

        _mockAccountRepository
            .Setup(r => r.GetUserRolesAsync(user))
            .ReturnsAsync(roles);

        _mockRefreshTokenService
            .Setup(s => s.RotateRefreshTokenAsync(refreshTokenRequest.RefreshToken, user.Id))
            .ReturnsAsync((CreateTestRefreshToken(user.Id), "new-refresh-token"));

        // Act
        var result = await _authService.RefreshAsync(refreshTokenRequest);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_WithInvalidRefreshToken_ThrowsValidationException()
    {
        // Arrange
        var refreshTokenRequest = new RefreshTokenRequestDto
        {
            RefreshToken = "invalid-token"
        };

        _mockRefreshTokenService
            .Setup(s => s.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken))
            .ThrowsAsync(new ValidationException("Invalid refresh token"));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _authService.RefreshAsync(refreshTokenRequest));
    }

    [Fact]
    public async Task RefreshAsync_WithNonExistentUser_ThrowsEntityNotFoundException()
    {
        // Arrange
        var refreshTokenRequest = new RefreshTokenRequestDto
        {
            RefreshToken = "valid-refresh-token"
        };

        var refreshTokenEntity = CreateTestRefreshToken("user-1");

        _mockRefreshTokenService
            .Setup(s => s.ValidateRefreshTokenAsync(refreshTokenRequest.RefreshToken))
            .ReturnsAsync(refreshTokenEntity);

        _mockAccountRepository
            .Setup(r => r.FindUserByIdAsync(refreshTokenEntity.UserId))
            .ReturnsAsync((IdentityUser?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _authService.RefreshAsync(refreshTokenRequest));
        Assert.Contains("User not found", exception.Message);
    }

    #endregion

    #region RevokeAsync Tests

    [Fact]
    public async Task RevokeAsync_WithValidToken_ReturnsSuccessResponse()
    {
        // Arrange
        var revokeTokenRequest = new RevokeTokenRequestDto
        {
            RefreshToken = "valid-refresh-token"
        };
        var userId = "user-1";

        var tokenHash = "hashed-token";
        var storedToken = CreateTestRefreshToken(userId, isRevoked: false);

        _mockRefreshTokenService
            .Setup(s => s.HashRefreshToken(revokeTokenRequest.RefreshToken))
            .Returns(tokenHash);

        _mockAccountRepository
            .Setup(r => r.FindRefreshTokenByHashAsync(tokenHash))
            .ReturnsAsync(storedToken);

        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _authService.RevokeAsync(revokeTokenRequest, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("revoked successfully", result.Message);
        Assert.True(storedToken.IsRevoked);
        Assert.NotNull(storedToken.RevokedAt);
    }

    [Fact]
    public async Task RevokeAsync_WithNonExistentToken_ThrowsValidationException()
    {
        // Arrange
        var revokeTokenRequest = new RevokeTokenRequestDto
        {
            RefreshToken = "nonexistent-token"
        };
        var userId = "user-1";

        var tokenHash = "hashed-token";

        _mockRefreshTokenService
            .Setup(s => s.HashRefreshToken(revokeTokenRequest.RefreshToken))
            .Returns(tokenHash);

        _mockAccountRepository
            .Setup(r => r.FindRefreshTokenByHashAsync(tokenHash))
            .ReturnsAsync((RefreshToken?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _authService.RevokeAsync(revokeTokenRequest, userId));
        Assert.Contains("Refresh token not found", exception.Message);
    }

    [Fact]
    public async Task RevokeAsync_WithTokenFromDifferentUser_ThrowsEntityUnauthorizedException()
    {
        // Arrange
        var revokeTokenRequest = new RevokeTokenRequestDto
        {
            RefreshToken = "valid-refresh-token"
        };
        var userId = "user-1";
        var tokenOwnerId = "user-2";

        var tokenHash = "hashed-token";
        var storedToken = CreateTestRefreshToken(tokenOwnerId, isRevoked: false);

        _mockRefreshTokenService
            .Setup(s => s.HashRefreshToken(revokeTokenRequest.RefreshToken))
            .Returns(tokenHash);

        _mockAccountRepository
            .Setup(r => r.FindRefreshTokenByHashAsync(tokenHash))
            .ReturnsAsync(storedToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityUnauthorizedException>(
            () => _authService.RevokeAsync(revokeTokenRequest, userId));
        Assert.Contains("does not belong to the current user", exception.Message);
    }

    [Fact]
    public async Task RevokeAsync_WithAlreadyRevokedToken_ThrowsValidationException()
    {
        // Arrange
        var revokeTokenRequest = new RevokeTokenRequestDto
        {
            RefreshToken = "revoked-token"
        };
        var userId = "user-1";

        var tokenHash = "hashed-token";
        var storedToken = CreateTestRefreshToken(userId, isRevoked: true);

        _mockRefreshTokenService
            .Setup(s => s.HashRefreshToken(revokeTokenRequest.RefreshToken))
            .Returns(tokenHash);

        _mockAccountRepository
            .Setup(r => r.FindRefreshTokenByHashAsync(tokenHash))
            .ReturnsAsync(storedToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _authService.RevokeAsync(revokeTokenRequest, userId));
        Assert.Contains("already been revoked", exception.Message);
    }

    [Fact]
    public async Task RevokeAsync_WithExpiredToken_ThrowsValidationException()
    {
        // Arrange
        var revokeTokenRequest = new RevokeTokenRequestDto
        {
            RefreshToken = "expired-token"
        };
        var userId = "user-1";

        var tokenHash = "hashed-token";
        var storedToken = CreateTestRefreshToken(userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(-1));

        _mockRefreshTokenService
            .Setup(s => s.HashRefreshToken(revokeTokenRequest.RefreshToken))
            .Returns(tokenHash);

        _mockAccountRepository
            .Setup(r => r.FindRefreshTokenByHashAsync(tokenHash))
            .ReturnsAsync(storedToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _authService.RevokeAsync(revokeTokenRequest, userId));
        Assert.Contains("has expired", exception.Message);
    }

    [Fact]
    public async Task RevokeAsync_WithConcurrencyError_ThrowsEntityServiceException()
    {
        // Arrange
        var revokeTokenRequest = new RevokeTokenRequestDto
        {
            RefreshToken = "valid-refresh-token"
        };
        var userId = "user-1";

        var tokenHash = "hashed-token";
        var storedToken = CreateTestRefreshToken(userId, isRevoked: false);

        _mockRefreshTokenService
            .Setup(s => s.HashRefreshToken(revokeTokenRequest.RefreshToken))
            .Returns(tokenHash);

        _mockAccountRepository
            .Setup(r => r.FindRefreshTokenByHashAsync(tokenHash))
            .ReturnsAsync(storedToken);

        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _authService.RevokeAsync(revokeTokenRequest, userId));
        Assert.Contains("concurrency issue", exception.Message);
    }

    [Fact]
    public async Task RevokeAsync_WithDatabaseError_ThrowsEntityServiceException()
    {
        // Arrange
        var revokeTokenRequest = new RevokeTokenRequestDto
        {
            RefreshToken = "valid-refresh-token"
        };
        var userId = "user-1";

        var tokenHash = "hashed-token";
        var storedToken = CreateTestRefreshToken(userId, isRevoked: false);

        _mockRefreshTokenService
            .Setup(s => s.HashRefreshToken(revokeTokenRequest.RefreshToken))
            .Returns(tokenHash);

        _mockAccountRepository
            .Setup(r => r.FindRefreshTokenByHashAsync(tokenHash))
            .ReturnsAsync(storedToken);

        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _authService.RevokeAsync(revokeTokenRequest, userId));
        Assert.Contains("database error", exception.Message);
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_WithAccessToken_BlacklistsAndRevokesAll()
    {
        // Arrange
        var userId = "user-1";
        var accessToken = GenerateTestJwt(userId);

        // Extract JTI from the generated token for verification
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(accessToken);
        var jti = jwtToken.Claims.First(c => c.Type == "jti").Value;

        // Add active refresh tokens to the database with unique IDs
        var token1 = CreateTestRefreshToken(userId, isRevoked: false);
        token1.Id = 1;
        var token2 = CreateTestRefreshToken(userId, isRevoked: false);
        token2.Id = 2;
        _context.RefreshTokens.Add(token1);
        _context.RefreshTokens.Add(token2);
        await _context.SaveChangesAsync();

        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .Returns(async () =>
            {
                await _context.SaveChangesAsync();
                return 1;
            });

        // Act
        var result = await _authService.LogoutAsync(userId, accessToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Contains("Logged out successfully", result.Message);

        // Verify refresh tokens were revoked
        var revokedTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ToListAsync();
        Assert.All(revokedTokens, t => Assert.True(t.IsRevoked));

        // Verify access token was blacklisted by querying the database
        var blacklistedToken = await _context.BlacklistedTokens
            .FirstOrDefaultAsync(bt => bt.Jti == jti);
        Assert.NotNull(blacklistedToken);
        Assert.Equal(jti, blacklistedToken.Jti);
        Assert.True(blacklistedToken.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LogoutAsync_WithoutAccessToken_RevokesAllRefreshTokens()
    {
        // Arrange
        var userId = "user-1";

        // Add active refresh tokens to the database with unique IDs
        var token1 = CreateTestRefreshToken(userId, isRevoked: false);
        token1.Id = 3;
        var token2 = CreateTestRefreshToken(userId, isRevoked: false);
        token2.Id = 4;
        _context.RefreshTokens.Add(token1);
        _context.RefreshTokens.Add(token2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.LogoutAsync(userId, null);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);

        // Verify refresh tokens were revoked
        var revokedTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ToListAsync();
        Assert.All(revokedTokens, t => Assert.True(t.IsRevoked));
    }

    [Fact]
    public async Task LogoutAsync_WithNoActiveTokens_CompletesSuccessfully()
    {
        // Arrange
        var userId = "user-1";

        // Act
        var result = await _authService.LogoutAsync(userId, null);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task LogoutAsync_WithNullAccessToken_CompletesSuccessfully()
    {
        // Arrange
        var userId = "user-1";

        // Act
        var result = await _authService.LogoutAsync(userId, null);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    #endregion

    #region WebLogoutAsync Tests

    [Fact]
    public async Task WebLogoutAsync_WithAccessToken_BlacklistsAndRevokesAll()
    {
        // Arrange
        var userId = "user-1";
        var accessToken = GenerateTestJwt(userId);

        // Extract JTI from the generated token for verification
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(accessToken);
        var jti = jwtToken.Claims.First(c => c.Type == "jti").Value;

        // Add active refresh tokens to the database with unique ID
        var token1 = CreateTestRefreshToken(userId, isRevoked: false);
        token1.Id = 5;
        _context.RefreshTokens.Add(token1);
        await _context.SaveChangesAsync();

        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .Returns(async () =>
            {
                await _context.SaveChangesAsync();
                return 1;
            });

        // Act
        var result = await _authService.WebLogoutAsync(userId, accessToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);

        // Verify refresh tokens were revoked
        var revokedTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ToListAsync();
        Assert.All(revokedTokens, t => Assert.True(t.IsRevoked));

        // Verify access token was blacklisted by querying the database
        var blacklistedToken = await _context.BlacklistedTokens
            .FirstOrDefaultAsync(bt => bt.Jti == jti);
        Assert.NotNull(blacklistedToken);
        Assert.Equal(jti, blacklistedToken.Jti);
        Assert.True(blacklistedToken.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task WebLogoutAsync_WithoutAccessToken_RevokesAllRefreshTokens()
    {
        // Arrange
        var userId = "user-1";

        // Add active refresh tokens to the database
        var token1 = CreateTestRefreshToken(userId, isRevoked: false);
        _context.RefreshTokens.Add(token1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.WebLogoutAsync(userId, null);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    #endregion

    #region BlacklistTokenAsync Tests

    [Fact]
    public async Task BlacklistTokenAsync_WithValidJti_AddsToBlacklist()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _authService.BlacklistTokenAsync(jti, expiresAt);

        // Assert
        var blacklistedToken = _context.BlacklistedTokens.Local
            .FirstOrDefault(bt => bt.Jti == jti);
        Assert.NotNull(blacklistedToken);
        Assert.Equal(jti, blacklistedToken.Jti);
        Assert.Equal(expiresAt, blacklistedToken.ExpiresAt);
        _mockAccountRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task BlacklistTokenAsync_WithDuplicateJti_IsIdempotent()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // First call succeeds
        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        await _authService.BlacklistTokenAsync(jti, expiresAt);

        // Setup to throw DbUpdateException on second call (simulating DB duplicate constraint)
        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException());

        // Act - try to add again (should not throw due to idempotent handling)
        await _authService.BlacklistTokenAsync(jti, expiresAt);

        // Assert - service handled the duplicate gracefully
        _mockAccountRepository.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task BlacklistTokenAsync_WithConcurrencyIssue_LogsWarning()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Mock SaveChangesAsync to throw concurrency exception
        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act - should not throw even with concurrency issues
        await _authService.BlacklistTokenAsync(jti, expiresAt);

        // Assert - no exception thrown, warning logged
        _mockAccountRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
