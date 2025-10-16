using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Response;
using System.Security.Claims;


namespace attendance.testproject.Controllers_Testing;

public class AccountControllerTest
{
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<ILogger<AccountController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ICookieOptionsService> _mockCookieOptionsService;
    private readonly AccountController _accountController;

    public AccountControllerTest()
    {
        _mockAccountService = new Mock<IAccountService>();
        _mockLogger = new Mock<ILogger<AccountController>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockCookieOptionsService = new Mock<ICookieOptionsService>();
        _accountController = new AccountController(
            _mockAccountService.Object,
            _mockLogger.Object,
            _mockConfiguration.Object,
            _mockCookieOptionsService.Object);
    }

    #region Register Tests

    [Fact]
        public async Task Register_ReturnsOk_WhenRegistrationSuccessful()
    {
        // Arrange
        var registerDto = new RegisterDto { Username = "testuser", Password = "Test@123", Email = "test@test.com", Role = "Student", SectionId = 1 };
        var identityResult = IdentityResult.Success;
        var response = new RegisterResponseDto { Success = true, Message = "User registered successfully" };
        _mockAccountService.Setup(s => s.RegisterAsync(registerDto)).ReturnsAsync((identityResult, response));

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<RegisterResponseDto>(okResult.Value);
        Assert.True(responseDto.Success);
        _mockAccountService.Verify(s => s.RegisterAsync(registerDto), Times.Once);
    }
    #endregion

    #region Check Tests

    [Fact]
    public void Check_ReturnsOk_WhenUserAuthenticated()
    {
        // Arrange
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user123"), new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        // Act
        var result = _accountController.Check();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<CheckAuthResponseDto>(okResult.Value);
        Assert.True(responseDto.Success);
        Assert.Equal("testuser", responseDto.User);
    }

    #endregion
}