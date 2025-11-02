using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services;

namespace attendance_monitoring.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController(IAccountService accountService, ILogger<AccountController> logger, ICookieOptionsService cookieOptionsService)
        : ControllerBase
    {
        #region Endpoints

        #region POST: api/account/register
        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="registerDto">User registration data</param>
        /// <returns>Registration result</returns>
        /// <response code="200">User registered successfully</response>
        /// <response code="400">Invalid input data</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RegisterResponseDto>> Register(RegisterDto registerDto)
        {
            logger.LogInformation("Registration attempt for username: {Username}", registerDto.Username);

            // Validate that SectionId is provided for student registrations
            // Only check for students (default role is Student if not specified)
            // Instructor is an alias for Teacher, so treat it as such
            var roleToCheck = string.IsNullOrEmpty(registerDto.Role) ? "Student" : registerDto.Role;
            if (roleToCheck.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
            {
                roleToCheck = "Teacher";
            }

            if (roleToCheck.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                if (!registerDto.SectionId.HasValue || registerDto.SectionId <= 0)
                {
                    logger.LogWarning("Student registration failed for username: {Username}: Valid SectionId is required", registerDto.Username);
                    return BadRequest(new RegisterResponseDto { Success = false, Message = "Valid SectionId is required for student registration" });
                }
            }

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Registration failed due to invalid model state for username: {Username}", registerDto.Username);
                return BadRequest(new RegisterResponseDto { Success = false, Message = "Invalid request data" });
            }

            var (result, response) = await accountService.RegisterAsync(registerDto);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    logger.LogError("Error during user registration for {Username}: {ErrorDescription}", registerDto.Username, error.Description);
                }
                return BadRequest(new RegisterResponseDto { Success = false, Message = string.Join("; ", result.Errors.Select(e => e.Description)) });
            }

            logger.LogInformation("User registered successfully: {Username}", registerDto.Username);
            return Ok(new RegisterResponseDto { Success = true, Message = response?.Message ?? "User registered successfully" });
        }
        #endregion

        #region POST: api/account/login
        /// <summary>
        /// Authenticate user and return access/refresh tokens
        /// </summary>
        /// <param name="loginDto">User login credentials</param>
        /// <returns>JWT tokens</returns>
        /// <response code="200">Login successful</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="400">Invalid input data</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto loginDto)
        {
            logger.LogInformation("Login attempt for identifier: {Identifier}", loginDto.Username);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Login failed due to invalid model state for identifier: {Identifier}", loginDto.Username);
                return BadRequest(new LoginResponseDto { Success = false, Message = "Invalid request data" });
            }

            var (tokenResponse, username, role, error) = await accountService.LoginAsync(loginDto);

            if (tokenResponse == null)
            {
                logger.LogWarning("Login failed for identifier {Identifier}: {Error}", loginDto.Username, error);
                return Unauthorized(new LoginResponseDto { Success = false, Message = error ?? "Login failed" });
            }

            logger.LogInformation("User logged in successfully");
            return Ok(new LoginResponseDto
            {
                Success = true,
                Message = "Login successful",
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                User = username ?? string.Empty,
                Role = role ?? string.Empty
            });
        }
        #endregion

        #region POST: api/account/web/login
        /// <summary>
        /// Authenticate user and return access/refresh tokens (http-only cookies)
        /// </summary>
        /// <param name="webLoginDto">User login credentials with email or username</param>
        /// <returns>JWT tokens</returns>
        /// <response code="200">Login successful</response>
        /// <response code="401">Invalid credentials</response>
        /// <response code="400">Invalid input data</response>
        [HttpPost("web/login")]
        [ProducesResponseType(typeof(WebLoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(WebLoginResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(WebLoginResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<WebLoginResponseDto>> WebLogin(WebLoginDto webLoginDto)
        {
            logger.LogInformation("Web login attempt for identifier: {Identifier}", webLoginDto.Identifier);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Web login failed due to invalid model state for identifier: {Identifier}", webLoginDto.Identifier);
                return BadRequest(new WebLoginResponseDto { Success = false, Message = "Invalid request data" });
            }

            // Map WebLoginDto to LoginDto for compatibility with existing LoginAsync method
            var loginDto = new LoginDto
            {
                Username = webLoginDto.Identifier,
                Password = webLoginDto.Password
            };

            var (tokenResponse, username, role, error) = await accountService.LoginAsync(loginDto);

            if (tokenResponse == null)
            {
                logger.LogWarning("Web login failed for identifier {Identifier}: {Error}", webLoginDto.Identifier, error);
                return Unauthorized(new WebLoginResponseDto { Success = false, Message = error ?? "An unexpected error occurred." });
            }

            // Set HTTP-only cookies for access and refresh tokens
            cookieOptionsService.SetTokenCookies(Response, tokenResponse.AccessToken, tokenResponse.RefreshToken);

            logger.LogInformation("User logged in successfully via web login");
            return Ok(new WebLoginResponseDto 
            { 
                Success = true, 
                Message = "Login successful",
                Username = username ?? string.Empty,
                Role = role ?? string.Empty
            });
        }
        #endregion

        #region POST: api/account/refresh
        /// <summary>
        /// Refresh tokens and optionally revoke the old access token
        /// </summary>
        /// <param name="refreshTokenRequest">Refresh token and optional old access token</param>
        /// <returns>New JWT tokens</returns>
        /// <response code="200">Tokens refreshed successfully</response>
        /// <response code="401">Invalid refresh token</response>
        /// <response code="400">Invalid input data</response>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(RefreshResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RefreshResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(RefreshResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RefreshResponseDto>> Refresh(RefreshTokenRequestDto refreshTokenRequest)
        {
            logger.LogInformation("Token refresh attempt.");
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Token refresh failed due to invalid model state.");
                return BadRequest(new RefreshResponseDto { Success = false, Message = "Invalid request data" });
            }

            var (tokenResponse, error) = await accountService.RefreshAsync(refreshTokenRequest);

            if (tokenResponse == null)
            {
                logger.LogWarning("Token refresh failed: {Error}", error);
                return Unauthorized(new RefreshResponseDto { Success = false, Message = error ?? "Token refresh failed" });
            }

            logger.LogInformation("Token refreshed successfully.");
            return Ok(new RefreshResponseDto
            {
                Success = true,
                Message = "Token refreshed successfully",
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken
            });
        }
        #endregion

        #region POST: api/account/web/refresh
        /// <summary>
        /// Refresh tokens using HTTP-only cookies
        /// </summary>
        /// <returns>New JWT tokens</returns>
        /// <response code="200">Tokens refreshed successfully</response>
        /// <response code="401">Invalid refresh token</response>
        [HttpPost("web/refresh")]
        [ProducesResponseType(typeof(WebRefreshResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(WebRefreshResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<WebRefreshResponseDto>> WebRefresh()
        {
            logger.LogInformation("Web token refresh attempt.");

            // Get refresh token from cookie
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
            {
                logger.LogWarning("Token refresh failed: No refresh token in cookies.");
                return Unauthorized(new WebRefreshResponseDto { Success = false, Message = "Refresh token not found" });
            }

            // Get old access token from cookie
            var oldAccessToken = Request.Cookies.TryGetValue("accessToken", out var accessToken) ? accessToken : null;

            var refreshTokenRequest = new RefreshTokenRequestDto
            {
                RefreshToken = refreshToken,
                OldAccessToken = oldAccessToken
            };
            var (tokenResponse, error) = await accountService.RefreshAsync(refreshTokenRequest);

            if (tokenResponse == null)
            {
                logger.LogWarning("Token refresh failed: {Error}", error);
                return Unauthorized(new WebRefreshResponseDto { Success = false, Message = error ?? "Token refresh failed" });
            }

            // Update HTTP-only cookies with new tokens
            cookieOptionsService.SetTokenCookies(Response, tokenResponse.AccessToken, tokenResponse.RefreshToken);

            logger.LogInformation("Web tokens refreshed successfully.");
            return Ok(new WebRefreshResponseDto { Success = true, Message = "Tokens refreshed successfully" });
        }
        #endregion

        #region POST: api/account/revoke
        /// <summary>
        /// Revoke a refresh token
        /// </summary>
        /// <param name="revokeTokenRequest">Refresh token to revoke</param>
        /// <returns>Revocation status</returns>
        /// <response code="200">Token revoked successfully</response>
        /// <response code="401">Invalid token or user</response>
        /// <response code="400">Invalid input data</response>
        [HttpPost("revoke")]
        [Authorize]
        [ProducesResponseType(typeof(RevokeResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RevokeResponseDto), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(RevokeResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RevokeResponseDto>> Revoke(RevokeTokenRequestDto revokeTokenRequest)
        {
            logger.LogInformation("Token revocation attempt.");
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Token revocation failed due to invalid model state.");
                return BadRequest(new RevokeResponseDto { Success = false, Message = "Invalid request data" });
            }

            var userId = GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Token revocation failed: User not found from claims.");
                return Unauthorized(new RevokeResponseDto { Success = false, Message = "User not found" });
            }

            var (response, error) = await accountService.RevokeAsync(revokeTokenRequest, userId);

            if (response == null)
            {
                logger.LogWarning("Token revocation failed: {Error}", error);
                return Unauthorized(new RevokeResponseDto { Success = false, Message = error ?? "Token revocation failed" });
            }

            logger.LogInformation("Refresh token revoked successfully for user {UserId}.", userId);
            return Ok(new RevokeResponseDto { Success = true, Message = response.Message });
        }
        #endregion

        #region POST: api/account/web/revoke
        /// <summary>
        /// Revoke refresh token using HTTP-only cookies
        /// </summary>
        /// <returns>Revocation status</returns>
        /// <response code="200">Token revoked successfully</response>
        /// <response code="401">Invalid token or user</response>
        [HttpPost("web/revoke")]
        [Authorize]
        [ProducesResponseType(typeof(WebRevokeResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(WebRevokeResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<WebRevokeResponseDto>> WebRevoke()
        {
            logger.LogInformation("Web token revocation attempt.");

            // Get refresh token from cookie
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
            {
                logger.LogWarning("Token revocation failed: No refresh token in cookies.");
                return Unauthorized(new WebRevokeResponseDto { Success = false, Message = "Refresh token not found" });
            }

            var userId = GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Token revocation failed: User not found from claims.");
                return Unauthorized(new WebRevokeResponseDto { Success = false, Message = "User not found" });
            }

            var revokeTokenRequest = new RevokeTokenRequestDto { RefreshToken = refreshToken };
            var (response, error) = await accountService.RevokeAsync(revokeTokenRequest, userId);

            if (response == null)
            {
                logger.LogWarning("Token revocation failed: {Error}", error);
                return Unauthorized(new WebRevokeResponseDto { Success = false, Message = error ?? "Token revocation failed" });
            }

            // Clear cookies after revocation
            cookieOptionsService.ClearTokenCookies(Response);

            logger.LogInformation("Refresh token revoked successfully for user {UserId}.", userId);
            return Ok(new WebRevokeResponseDto { Success = true, Message = "Token revoked successfully" });
        }
        #endregion

        #region GET: api/account/check
        /// <summary>
        /// Check if the user is authenticated
        /// </summary>
        /// <returns>Authentication status</returns>
        /// <response code="200">User is authenticated</response>
        /// <response code="401">User is not authenticated</response>
        [HttpGet("check")]
        [Authorize]
        [ProducesResponseType(typeof(CheckAuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(CheckAuthResponseDto), StatusCodes.Status401Unauthorized)]
        public ActionResult<CheckAuthResponseDto> Check()
        {
            logger.LogInformation("Authentication check for user: {UserId}", GetUserId(User));
            var username = GetUsername(User);
            return Ok(new CheckAuthResponseDto { Success = true, Message = "User is authenticated", User = username });
        }
        #endregion

        #region GET: api/account/me
        /// <summary>
        /// Get the current authenticated user's profile information
        /// </summary>
        /// <returns>User profile with role-specific data</returns>
        /// <response code="200">User profile retrieved successfully</response>
        /// <response code="401">User is not authenticated</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserProfileResponseDto>> GetMe()
        {
            var userId = GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Profile fetch failed: User not found from claims.");
                return Unauthorized(new { Success = false, Message = "User not found" });
            }

            logger.LogInformation("Fetching profile for user: {UserId}", userId);
            var (profile, error) = await accountService.GetUserProfileAsync(userId);

            if (profile == null)
            {
                logger.LogWarning("Profile fetch failed for user {UserId}: {Error}", userId, error);
                return Unauthorized(new { Success = false, Message = error ?? "Failed to fetch profile" });
            }

            logger.LogInformation("Profile fetched successfully for user: {UserId}", userId);
            return Ok(profile);
        }
        #endregion

        #region POST: api/account/web/logout
        /// <summary>
        /// Logout user by clearing HTTP-only cookies and blacklisting the access token
        /// </summary>
        /// <returns>Logout status</returns>
        /// <response code="200">User logged out successfully</response>
        [HttpPost("web/logout")]
        [Authorize]
        [ProducesResponseType(typeof(LogoutResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<LogoutResponseDto>> WebLogout()
        {
            var userId = GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Logout failed: User not found from claims.");
                // Always clear cookies and return success to prevent timing attacks
                cookieOptionsService.ClearTokenCookies(Response);
                return Ok(new LogoutResponseDto { Success = true, Message = "Logged out successfully" });
            }

            // Get access token from cookie
            var accessToken = Request.Cookies.TryGetValue("accessToken", out var token) ? token : null;

            // Always perform logout operations regardless of token validity to prevent timing attacks
            await accountService.WebLogoutAsync(userId, accessToken);

            // Always clear cookies
            cookieOptionsService.ClearTokenCookies(Response);

            logger.LogInformation("User logged out successfully: {UserId}", userId);
            return Ok(new LogoutResponseDto { Success = true, Message = "Logged out successfully" });
        }
        #endregion

        #region POST: api/account/logout
        /// <summary>
        /// Logout user by blacklisting the access token and revoking all refresh tokens
        /// </summary>
        /// <returns>Logout status</returns>
        /// <response code="200">User logged out successfully</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(LogoutResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<LogoutResponseDto>> Logout()
        {
            logger.LogInformation("JWT logout attempt.");

            var userId = GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Logout failed: User not found from claims.");
                // Always return success to prevent timing attacks
                return Ok(new LogoutResponseDto { Success = true, Message = "Logged out successfully" });
            }

            // Get access token from Authorization header
            string? accessToken = null;
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var headerValue = authHeader.ToString();
                if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    accessToken = headerValue["Bearer ".Length..].Trim();
                }
            }

            // Always perform logout operations regardless of token validity to prevent timing attacks
            await accountService.LogoutAsync(userId, accessToken);

            logger.LogInformation("User logged out successfully: {UserId}", userId);
            return Ok(new LogoutResponseDto { Success = true, Message = "Logged out successfully" });
        }
        #endregion

        #region PATCH: api/account/profile
        /// <summary>
        /// Update the authenticated user's own profile
        /// </summary>
        /// <param name="updateProfileDto">Profile update data</param>
        /// <returns>Updated profile information</returns>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid input data or validation error</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="404">User not found</response>
        [Authorize]
        [HttpPatch("profile")]
        [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UpdateProfileResponse>> UpdateProfile(Models.DTO.Request.UpdateProfile updateProfileDto)
        {
            logger.LogInformation("Profile update request received.");

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Profile update failed due to invalid model state.");
                return BadRequest(new UpdateProfileResponse { Success = false, Message = "Invalid request data" });
            }

            var userId = GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Profile update failed: User not found from claims.");
                return Unauthorized(new UpdateProfileResponse { Success = false, Message = "User not authenticated" });
            }

            var (success, profile, errorMessage) = await accountService.UpdateUserProfileAsync(userId, updateProfileDto);

            if (!success)
            {
                logger.LogWarning("Profile update failed for user {UserId}: {Error}", userId, errorMessage);

                // Return 404 if user not found, 400 for validation errors
                if (errorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new UpdateProfileResponse { Success = false, Message = errorMessage });
                }

                return BadRequest(new UpdateProfileResponse { Success = false, Message = errorMessage ?? "Profile update failed" });
            }

            logger.LogInformation("Profile updated successfully for user {UserId}.", userId);
            return Ok(new UpdateProfileResponse
            {
                Success = true,
                Message = "Profile updated successfully",
                UpdatedProfile = profile
            });
        }
        #endregion

        #region PATCH: api/account/admin/users/{userId}
        /// <summary>
        /// Admin endpoint to update another user's profile
        /// </summary>
        /// <param name="userId">Target user ID to update</param>
        /// <param name="adminUpdateDto">Profile update data</param>
        /// <returns>Updated profile information</returns>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid input data or validation error</response>
        /// <response code="401">User not authenticated</response>
        /// <response code="403">User is not an admin</response>
        /// <response code="404">Target user not found</response>
        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/users/{userId}")]
        [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(UpdateProfileResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UpdateProfileResponse>> AdminUpdateUser(string userId, Models.DTO.Request.AdminUpdateUser adminUpdateDto)
        {
            logger.LogInformation("Admin profile update request received for user {TargetUserId}.", userId);

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Admin profile update failed due to invalid model state.");
                return BadRequest(new UpdateProfileResponse { Success = false, Message = "Invalid request data" });
            }

            var adminId = GetUserId(User);
            if (string.IsNullOrEmpty(adminId))
            {
                logger.LogWarning("Admin profile update failed: Admin not found from claims.");
                return Unauthorized(new UpdateProfileResponse { Success = false, Message = "Admin not authenticated" });
            }

            // Override DTO userId with route parameter to prevent mismatch
            adminUpdateDto.UserId = userId;

            var (success, profile, errorMessage) = await accountService.AdminUpdateUserProfileAsync(adminId, adminUpdateDto);

            if (!success)
            {
                logger.LogWarning("Admin profile update failed for user {TargetUserId}: {Error}", userId, errorMessage);

                // Return appropriate status code based on error
                if (errorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new UpdateProfileResponse { Success = false, Message = errorMessage });
                }

                if (errorMessage?.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new UpdateProfileResponse { Success = false, Message = errorMessage });
                }

                return BadRequest(new UpdateProfileResponse { Success = false, Message = errorMessage ?? "Profile update failed" });
            }

            logger.LogInformation("Admin {AdminId} successfully updated profile for user {TargetUserId}.", adminId, userId);
            return Ok(new UpdateProfileResponse
            {
                Success = true,
                Message = "Profile updated successfully by admin",
                UpdatedProfile = profile
            });
        }
        #endregion

        #endregion

        #region Private Methods
        private string? GetUserId(ClaimsPrincipal userPrincipal)
        {
            return userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private string? GetUsername(ClaimsPrincipal userPrincipal)
        {
            return userPrincipal.FindFirst(ClaimTypes.Name)?.Value;
        }
        #endregion
    }
}
