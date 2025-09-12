using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(IAccountService accountService, ILogger<AccountController> logger)
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
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RegisterResponseDto>> Register(RegisterDto registerDto)
        {
            logger.LogInformation("Registration attempt for username: {Username}", registerDto.Username);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Registration failed due to invalid model state for username: {Username}", registerDto.Username);
                return BadRequest(ModelState);
            }

            var (result, response) = await accountService.RegisterAsync(registerDto);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    logger.LogError("Error during user registration for {Username}: {ErrorDescription}", registerDto.Username, error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            logger.LogInformation("User registered successfully: {Username}", registerDto.Username);
            return Ok(response);
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
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TokenResponseDto>> Login(LoginDto loginDto)
        {
            logger.LogInformation("Login attempt for username: {Username}", loginDto.Username);
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Login failed due to invalid model state for username: {Username}", loginDto.Username);
                return BadRequest(ModelState);
            }

            var (tokenResponse, error) = await accountService.LoginAsync(loginDto);

            if (tokenResponse == null)
            {
                logger.LogWarning("Login failed for username {Username}: {Error}", loginDto.Username, error);
                ModelState.AddModelError("Error", error ?? "An unexpected error occurred.");
                return Unauthorized(ModelState);
            }

            logger.LogInformation("User {Username} logged in successfully", loginDto.Username);
            return Ok(tokenResponse);
        }
        #endregion

        #region POST: api/account/refresh
        // POST: api/account/refresh
        [HttpPost("refresh")]
        public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshTokenRequestDto refreshTokenRequest)
        {
            logger.LogInformation("Token refresh attempt.");
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Token refresh failed due to invalid model state.");
                return BadRequest(ModelState);
            }

            var (tokenResponse, error) = await accountService.RefreshAsync(refreshTokenRequest);

            if (tokenResponse == null)
            {
                logger.LogWarning("Token refresh failed: {Error}", error);
                return Unauthorized(new { Message = error });
            }

            logger.LogInformation("Token refreshed successfully.");
            return Ok(tokenResponse);
        }
        #endregion

        #region POST: api/account/revoke
        // POST: api/account/revoke
        [HttpPost("revoke")]
        [Authorize]
        [ProducesResponseType(typeof(RevokeResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<RevokeResponseDto>> Revoke(RevokeTokenRequestDto revokeTokenRequest)
        {
            logger.LogInformation("Token revocation attempt.");
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Token revocation failed due to invalid model state.");
                return BadRequest(ModelState);
            }

            var userId = GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Token revocation failed: User not found from claims.");
                return Unauthorized(new { Message = "User not found" });
            }

            var (response, error) = await accountService.RevokeAsync(revokeTokenRequest, userId);

            if (response == null)
            {
                logger.LogWarning("Token revocation failed: {Error}", error);
                return Unauthorized(new { Message = error });
            }

            logger.LogInformation("Refresh token revoked successfully for user {UserId}.", userId);
            return Ok(response);
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
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status401Unauthorized)]
        public ActionResult<CheckAuthResponseDto> Check()
        {
            logger.LogInformation("Authentication check for user: {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return Ok(new CheckAuthResponseDto { Message = "User is authenticated", User = User.Identity?.Name });
        }
        #endregion

        #endregion

        #region Private Methods
        private string? GetUserId(ClaimsPrincipal userPrincipal)
        {
            return userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        #endregion
    }
}
