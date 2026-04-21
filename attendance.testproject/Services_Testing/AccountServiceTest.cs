using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services;
using attendance_monitoring.Services.Account;

namespace attendance.testproject.Services_Testing;

public class AccountServiceTest
{
    private readonly FakeRegistrationService _registrationService;
    private readonly FakeAuthenticationService _authenticationService;
    private readonly FakeProfileService _profileService;
    private readonly FakeAdminService _adminService;
    private readonly AccountService _service;

    public AccountServiceTest()
    {
        _registrationService = new FakeRegistrationService();
        _authenticationService = new FakeAuthenticationService();
        _profileService = new FakeProfileService();
        _adminService = new FakeAdminService();

        _service = new AccountService(
            _registrationService,
            _authenticationService,
            _profileService,
            _adminService);
    }

    [Fact]
    public void Constructor_NullRegistrationService_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new AccountService(null!, _authenticationService, _profileService, _adminService));

        Assert.Equal("registrationService", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullAuthenticationService_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new AccountService(_registrationService, null!, _profileService, _adminService));

        Assert.Equal("authenticationService", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullProfileService_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new AccountService(_registrationService, _authenticationService, null!, _adminService));

        Assert.Equal("profileService", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullAdminService_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new AccountService(_registrationService, _authenticationService, _profileService, null!));

        Assert.Equal("adminService", exception.ParamName);
    }

    [Fact]
    public async Task GetAllUsersAsync_ForwardsStatusToAdminService()
    {
        var users = new List<GetAllUsersDto> { new() { UserId = "user-1", Username = "alpha" } };
        _adminService.GetAllUsersResult = users;

        var result = await _service.GetAllUsersAsync(UserStatus.Archived);

        Assert.Same(users, result);
        Assert.Equal(UserStatus.Archived, _adminService.LastStatus);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsRegistrationResult()
    {
        var registerDto = new RegisterDto { Username = "user", Email = "user@test.com", Password = "secret", RepeatedPassword = "secret" };
        var expected = new RegisterResponseDto { Success = true, Message = "ok" };
        _registrationService.RegisterResult = expected;

        var result = await _service.RegisterAsync(registerDto);

        Assert.Same(expected, result);
        Assert.Same(registerDto, _registrationService.LastRegisterDto);
    }

    [Fact]
    public async Task LoginAsync_ReturnsAuthenticationResult()
    {
        var loginDto = new LoginDto { Username = "user", Password = "secret" };
        var expected = new LoginResult
        {
            Username = "user",
            Role = "Admin",
            TokenResponse = new TokenResponseDto { AccessToken = "access", RefreshToken = "refresh" },
        };
        _authenticationService.LoginResult = expected;

        var result = await _service.LoginAsync(loginDto);

        Assert.Same(expected, result);
        Assert.Same(loginDto, _authenticationService.LastLoginDto);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsAuthenticationResult()
    {
        var request = new RefreshTokenRequestDto { RefreshToken = "refresh" };
        var expected = new TokenResponseDto { AccessToken = "access", RefreshToken = "refresh-2" };
        _authenticationService.RefreshResult = expected;

        var result = await _service.RefreshAsync(request);

        Assert.Same(expected, result);
        Assert.Same(request, _authenticationService.LastRefreshRequest);
    }

    [Fact]
    public async Task RevokeAsync_ForwardsArgumentsToAuthenticationService()
    {
        var request = new RevokeTokenRequestDto { RefreshToken = "refresh" };
        var expected = new RevokeResponseDto { Success = true, Message = "revoked" };
        _authenticationService.RevokeResult = expected;

        var result = await _service.RevokeAsync(request, "user-1");

        Assert.Same(expected, result);
        Assert.Same(request, _authenticationService.LastRevokeRequest);
        Assert.Equal("user-1", _authenticationService.LastRevokeUserId);
    }

    [Fact]
    public async Task LogoutAsync_ReturnsAuthenticationResult()
    {
        var expected = new LogoutResponseDto { Success = true, Message = "logged out" };
        _authenticationService.LogoutResult = expected;

        var result = await _service.LogoutAsync("user-1", "token");

        Assert.Same(expected, result);
        Assert.Equal("user-1", _authenticationService.LastLogoutUserId);
        Assert.Equal("token", _authenticationService.LastLogoutAccessToken);
    }

    [Fact]
    public async Task WebLogoutAsync_ReturnsAuthenticationResult()
    {
        var expected = new LogoutResponseDto { Success = true, Message = "logged out" };
        _authenticationService.WebLogoutResult = expected;

        var result = await _service.WebLogoutAsync("user-1", "token");

        Assert.Same(expected, result);
        Assert.Equal("user-1", _authenticationService.LastWebLogoutUserId);
        Assert.Equal("token", _authenticationService.LastWebLogoutAccessToken);
    }

    [Fact]
    public async Task BlacklistTokenAsync_ForwardsArgumentsToAuthenticationService()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        await _service.BlacklistTokenAsync("jti-1", expiresAt);

        Assert.Equal("jti-1", _authenticationService.LastBlacklistedJti);
        Assert.Equal(expiresAt, _authenticationService.LastBlacklistedExpiresAt);
    }

    [Fact]
    public async Task GetUserProfileAsync_ReturnsProfileResult()
    {
        var expected = new UserProfileResponseDto { UserId = "user-1", Username = "alpha" };
        _profileService.GetProfileResult = expected;

        var result = await _service.GetUserProfileAsync("user-1");

        Assert.Same(expected, result);
        Assert.Equal("user-1", _profileService.LastGetUserId);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ReturnsProfileResult()
    {
        var request = new UpdateProfile { Firstname = "Ada" };
        var expected = new UserProfileResponseDto { UserId = "user-1", Username = "alpha" };
        _profileService.UpdateProfileResult = expected;

        var result = await _service.UpdateUserProfileAsync("user-1", request);

        Assert.Same(expected, result);
        Assert.Equal("user-1", _profileService.LastUpdateUserId);
        Assert.Same(request, _profileService.LastUpdateProfile);
    }

    [Fact]
    public async Task AdminUpdateUserProfileAsync_ReturnsAdminResult()
    {
        var request = new AdminUpdateUser { UserId = "user-2", Firstname = "Grace" };
        var expected = new UserProfileResponseDto { UserId = "user-2", Username = "beta" };
        _adminService.AdminUpdateResult = expected;

        var result = await _service.AdminUpdateUserProfileAsync("admin-1", request);

        Assert.Same(expected, result);
        Assert.Equal("admin-1", _adminService.LastAdminUpdateAdminId);
        Assert.Same(request, _adminService.LastAdminUpdateUser);
    }

    [Fact]
    public async Task AdminDeleteUserAsync_ForwardsArgumentsToAdminService()
    {
        await _service.AdminDeleteUserAsync("admin-1", "user-2");

        Assert.Equal("admin-1", _adminService.LastDeleteAdminId);
        Assert.Equal("user-2", _adminService.LastDeleteTargetUserId);
    }

    [Fact]
    public async Task AdminHardDeleteUserAsync_ForwardsArgumentsToAdminService()
    {
        await _service.AdminHardDeleteUserAsync("admin-1", "user-2");

        Assert.Equal("admin-1", _adminService.LastHardDeleteAdminId);
        Assert.Equal("user-2", _adminService.LastHardDeleteTargetUserId);
    }

    [Fact]
    public async Task AdminRestoreUserAsync_ForwardsArgumentsToAdminService()
    {
        await _service.AdminRestoreUserAsync("admin-1", "user-2");

        Assert.Equal("admin-1", _adminService.LastRestoreAdminId);
        Assert.Equal("user-2", _adminService.LastRestoreTargetUserId);
    }

    private sealed class FakeRegistrationService : IRegistrationService
    {
        public RegisterDto? LastRegisterDto { get; private set; }
        public RegisterResponseDto RegisterResult { get; set; } = new();

        public Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            LastRegisterDto = registerDto;
            return Task.FromResult(RegisterResult);
        }
    }

    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        public LoginDto? LastLoginDto { get; private set; }
        public RefreshTokenRequestDto? LastRefreshRequest { get; private set; }
        public RevokeTokenRequestDto? LastRevokeRequest { get; private set; }
        public string? LastRevokeUserId { get; private set; }
        public string? LastLogoutUserId { get; private set; }
        public string? LastLogoutAccessToken { get; private set; }
        public string? LastWebLogoutUserId { get; private set; }
        public string? LastWebLogoutAccessToken { get; private set; }
        public string? LastBlacklistedJti { get; private set; }
        public DateTime LastBlacklistedExpiresAt { get; private set; }
        public LoginResult LoginResult { get; set; } = new()
        {
            Username = string.Empty,
            Role = string.Empty,
            TokenResponse = new TokenResponseDto(),
        };
        public TokenResponseDto RefreshResult { get; set; } = new();
        public RevokeResponseDto RevokeResult { get; set; } = new();
        public LogoutResponseDto LogoutResult { get; set; } = new();
        public LogoutResponseDto WebLogoutResult { get; set; } = new();

        public Task<LoginResult> LoginAsync(LoginDto loginDto)
        {
            LastLoginDto = loginDto;
            return Task.FromResult(LoginResult);
        }

        public Task<TokenResponseDto> RefreshAsync(RefreshTokenRequestDto refreshTokenRequest)
        {
            LastRefreshRequest = refreshTokenRequest;
            return Task.FromResult(RefreshResult);
        }

        public Task<RevokeResponseDto> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId)
        {
            LastRevokeRequest = revokeTokenRequest;
            LastRevokeUserId = userId;
            return Task.FromResult(RevokeResult);
        }

        public Task<LogoutResponseDto> LogoutAsync(string userId, string? accessToken)
        {
            LastLogoutUserId = userId;
            LastLogoutAccessToken = accessToken;
            return Task.FromResult(LogoutResult);
        }

        public Task<LogoutResponseDto> WebLogoutAsync(string userId, string? accessToken)
        {
            LastWebLogoutUserId = userId;
            LastWebLogoutAccessToken = accessToken;
            return Task.FromResult(WebLogoutResult);
        }

        public Task BlacklistTokenAsync(string jti, DateTime expiresAt)
        {
            LastBlacklistedJti = jti;
            LastBlacklistedExpiresAt = expiresAt;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProfileService : IProfileService
    {
        public string? LastGetUserId { get; private set; }
        public string? LastUpdateUserId { get; private set; }
        public UpdateProfile? LastUpdateProfile { get; private set; }
        public UserProfileResponseDto GetProfileResult { get; set; } = new();
        public UserProfileResponseDto UpdateProfileResult { get; set; } = new();

        public Task<UserProfileResponseDto> GetUserProfileAsync(string userId)
        {
            LastGetUserId = userId;
            return Task.FromResult(GetProfileResult);
        }

        public Task<UserProfileResponseDto> UpdateUserProfileAsync(string userId, UpdateProfile updateProfileDto)
        {
            LastUpdateUserId = userId;
            LastUpdateProfile = updateProfileDto;
            return Task.FromResult(UpdateProfileResult);
        }
    }

    private sealed class FakeAdminService : IAdminService
    {
        public UserStatus LastStatus { get; private set; }
        public IEnumerable<GetAllUsersDto> GetAllUsersResult { get; set; } = Array.Empty<GetAllUsersDto>();
        public string? LastAdminUpdateAdminId { get; private set; }
        public AdminUpdateUser? LastAdminUpdateUser { get; private set; }
        public UserProfileResponseDto AdminUpdateResult { get; set; } = new();
        public string? LastDeleteAdminId { get; private set; }
        public string? LastDeleteTargetUserId { get; private set; }
        public string? LastHardDeleteAdminId { get; private set; }
        public string? LastHardDeleteTargetUserId { get; private set; }
        public string? LastRestoreAdminId { get; private set; }
        public string? LastRestoreTargetUserId { get; private set; }

        public Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsync(UserStatus status = UserStatus.Active)
        {
            LastStatus = status;
            return Task.FromResult(GetAllUsersResult);
        }

        public Task<UserProfileResponseDto> AdminUpdateUserProfileAsync(string adminId, AdminUpdateUser adminUpdateDto)
        {
            LastAdminUpdateAdminId = adminId;
            LastAdminUpdateUser = adminUpdateDto;
            return Task.FromResult(AdminUpdateResult);
        }

        public Task AdminDeleteUserAsync(string adminId, string targetUserId)
        {
            LastDeleteAdminId = adminId;
            LastDeleteTargetUserId = targetUserId;
            return Task.CompletedTask;
        }

        public Task AdminHardDeleteUserAsync(string adminId, string targetUserId)
        {
            LastHardDeleteAdminId = adminId;
            LastHardDeleteTargetUserId = targetUserId;
            return Task.CompletedTask;
        }

        public Task AdminRestoreUserAsync(string adminId, string targetUserId)
        {
            LastRestoreAdminId = adminId;
            LastRestoreTargetUserId = targetUserId;
            return Task.CompletedTask;
        }
    }
}
