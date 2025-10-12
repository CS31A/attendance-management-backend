using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController(IAccountService accountService, ILogger<AccountController> logger, IConfiguration configuration)
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
            if ((string.IsNullOrEmpty(registerDto.Role) ||
                registerDto.Role.Equals("Student", StringComparison.OrdinalIgnoreCase)))
            {
                if (registerDto.SectionId <= 0)
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

            var (tokenResponse, error) = await accountService.LoginAsync(loginDto);

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
                User = loginDto.Username
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

            var (tokenResponse, error) = await accountService.LoginAsync(loginDto);

            if (tokenResponse == null)
            {
                logger.LogWarning("Web login failed for identifier {Identifier}: {Error}", webLoginDto.Identifier, error);
                return Unauthorized(new WebLoginResponseDto { Success = false, Message = error ?? "An unexpected error occurred." });
            }

            // Set HTTP-only cookies for access and refresh tokens
            var accessTokenExpirationMinutes = configuration.GetValue("CookieSettings:AccessTokenExpirationMinutes", 15);
            var refreshTokenExpirationDays = configuration.GetValue("CookieSettings:RefreshTokenExpirationDays", 7);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Set to true in production with HTTPS
                // SameSite = SameSiteMode.Strict,
                //SameSite = SameSiteMode.Lax,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes)
            };

            Response.Cookies.Append("accessToken", tokenResponse.AccessToken, cookieOptions);

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Set to true in production with HTTPS
                               // SameSite = SameSiteMode.Strict,
                               //SameSite = SameSiteMode.Lax,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(refreshTokenExpirationDays)
            };

            Response.Cookies.Append("refreshToken", tokenResponse.RefreshToken, refreshCookieOptions);

            logger.LogInformation("User logged in successfully via web login");
            return Ok(new WebLoginResponseDto { Success = true, Message = "Login successful" });
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
            var accessTokenExpirationMinutes = configuration.GetValue("CookieSettings:AccessTokenExpirationMinutes", 15);
            var refreshTokenExpirationDays = configuration.GetValue("CookieSettings:RefreshTokenExpirationDays", 7);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Set to true in production with HTTPS
                               // SameSite = SameSiteMode.Lax, // Changed this from strict breh
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes)
            };

            Response.Cookies.Append("accessToken", tokenResponse.AccessToken, cookieOptions);

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Set to true in production with HTTPS
                //SameSite = SameSiteMode.Lax,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(refreshTokenExpirationDays)
            };

            Response.Cookies.Append("refreshToken", tokenResponse.RefreshToken, refreshCookieOptions);

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
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");

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
                Response.Cookies.Delete("accessToken");
                Response.Cookies.Delete("refreshToken");
                return Ok(new LogoutResponseDto { Success = true, Message = "Logged out successfully" });
            }

            // Get access token from cookie
            var accessToken = Request.Cookies.TryGetValue("accessToken", out var token) ? token : null;
            
            // Always perform logout operations regardless of token validity to prevent timing attacks
            await accountService.WebLogoutAsync(userId, accessToken);

            // Always clear cookies
            Response.Cookies.Delete("accessToken");
            Response.Cookies.Delete("refreshToken");

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
