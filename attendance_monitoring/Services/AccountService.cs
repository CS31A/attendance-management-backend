using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services.Account;

namespace attendance_monitoring.Services;

/// <summary>
/// Public facade for account management operations used by controllers and other callers.
/// Delegates work to focused account units for registration, authentication, profile, and admin operations.
/// </summary>
public class AccountService : IAccountService
{
    private readonly RegistrationService _registrationService;
    private readonly AuthenticationService _authenticationService;
    private readonly ProfileService _profileService;
    private readonly AdminService _adminService;

    internal AccountService(
        RegistrationService registrationService,
        AuthenticationService authenticationService,
        ProfileService profileService,
        AdminService adminService)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
    }

    #region User Listing

    public Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsync(UserStatus status = UserStatus.Active)
        => _adminService.GetAllUsersAsync(status);

    #endregion

    #region Registration

    public Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto)
        => _registrationService.RegisterAsync(registerDto);

    #endregion

    #region Authentication

    public Task<LoginResult> LoginAsync(LoginDto loginDto)
        => _authenticationService.LoginAsync(loginDto);

    #endregion

    #region Token Management

    public Task<TokenResponseDto> RefreshAsync(RefreshTokenRequestDto refreshTokenRequest)
        => _authenticationService.RefreshAsync(refreshTokenRequest);

    public Task<RevokeResponseDto> RevokeAsync(RevokeTokenRequestDto revokeTokenRequest, string userId)
        => _authenticationService.RevokeAsync(revokeTokenRequest, userId);

    public Task<LogoutResponseDto> LogoutAsync(string userId, string? accessToken)
        => _authenticationService.LogoutAsync(userId, accessToken);

    public Task<LogoutResponseDto> WebLogoutAsync(string userId, string? accessToken)
        => _authenticationService.WebLogoutAsync(userId, accessToken);

    public Task BlacklistTokenAsync(string jti, DateTime expiresAt)
        => _authenticationService.BlacklistTokenAsync(jti, expiresAt);

    #endregion

    #region Profile

    public Task<UserProfileResponseDto> GetUserProfileAsync(string userId)
        => _profileService.GetUserProfileAsync(userId);

    public Task<UserProfileResponseDto> UpdateUserProfileAsync(string userId, UpdateProfile updateProfileDto)
        => _profileService.UpdateUserProfileAsync(userId, updateProfileDto);

    #endregion

    #region Admin Operations

    public Task<UserProfileResponseDto> AdminUpdateUserProfileAsync(string adminId, AdminUpdateUser adminUpdateDto)
        => _adminService.AdminUpdateUserProfileAsync(adminId, adminUpdateDto);

    public Task AdminDeleteUserAsync(string adminId, string targetUserId)
        => _adminService.AdminDeleteUserAsync(adminId, targetUserId);

    public Task AdminHardDeleteUserAsync(string adminId, string targetUserId)
        => _adminService.AdminHardDeleteUserAsync(adminId, targetUserId);

    public Task AdminRestoreUserAsync(string adminId, string targetUserId)
        => _adminService.AdminRestoreUserAsync(adminId, targetUserId);

    #endregion
}
