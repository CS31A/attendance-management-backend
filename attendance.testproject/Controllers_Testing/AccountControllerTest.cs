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
    private readonly Mock<ICookieOptionsService> _mockCookieOptionsService;
    private readonly AccountController _accountController;

    public AccountControllerTest()
    {
        _mockAccountService = new Mock<IAccountService>();
        _mockLogger = new Mock<ILogger<AccountController>>();
        _mockCookieOptionsService = new Mock<ICookieOptionsService>();
        _accountController = new AccountController(
            _mockAccountService.Object,
            _mockLogger.Object,
            _mockCookieOptionsService.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_ReturnsOk_WhenRegistrationSuccessful()
    {
        // Arrange
        var registerDto = new RegisterDto { Username = "testuser", Password = "Test@123", Email = "test@test.com", RepeatedPassword = "Test@123", Role = "Student", SectionId = 1 };
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

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenStudentRegistrationWithoutSectionId()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Password = "Test@123",
            Email = "test@test.com",
            RepeatedPassword = "Test@123",
            Role = "Student"
            // No SectionId provided
        };

        // Manually trigger validation context to simulate ModelState validation
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(registerDto);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(registerDto, validationContext, validationResults, true);

        foreach (var validationResult in validationResults)
        {
            _accountController.ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage ?? "Validation error");
        }

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert - ModelState should have validation error
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenStudentRegistrationWithInvalidSectionId()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Password = "Test@123",
            Email = "test@test.com",
            RepeatedPassword = "Test@123",
            Role = "Student",
            SectionId = 0 // Invalid section ID
        };

        // Manually trigger validation context to simulate ModelState validation
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(registerDto);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(registerDto, validationContext, validationResults, true);

        foreach (var validationResult in validationResults)
        {
            _accountController.ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage ?? "Validation error");
        }

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Register_ReturnsOk_WhenTeacherRegistrationWithoutSectionId()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "teacheruser",
            Password = "Test@123",
            Email = "teacher@test.com",
            RepeatedPassword = "Test@123",
            Role = "Teacher"
            // No SectionId - this is valid for teachers
        };
        var identityResult = IdentityResult.Success;
        var response = new RegisterResponseDto { Success = true, Message = "User registered successfully" };
        _mockAccountService.Setup(s => s.RegisterAsync(registerDto)).ReturnsAsync((identityResult, response));

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<RegisterResponseDto>(okResult.Value);
        Assert.True(responseDto.Success);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenTeacherRegistrationWithSectionId()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "teacheruser",
            Password = "Test@123",
            Email = "teacher@test.com",
            RepeatedPassword = "Test@123",
            Role = "Teacher",
            SectionId = 1 // Invalid - teachers should not have section ID
        };

        // Manually trigger validation context to simulate ModelState validation
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(registerDto);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(registerDto, validationContext, validationResults, true);

        foreach (var validationResult in validationResults)
        {
            _accountController.ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage ?? "Validation error");
        }

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Register_ReturnsOk_WhenAdminRegistrationWithoutSectionId()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "adminuser",
            Password = "Test@123",
            Email = "admin@test.com",
            RepeatedPassword = "Test@123",
            Role = "Admin"
            // No SectionId - this is valid for admins
        };
        var identityResult = IdentityResult.Success;
        var response = new RegisterResponseDto { Success = true, Message = "User registered successfully" };
        _mockAccountService.Setup(s => s.RegisterAsync(registerDto)).ReturnsAsync((identityResult, response));

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<RegisterResponseDto>(okResult.Value);
        Assert.True(responseDto.Success);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenAdminRegistrationWithSectionId()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "adminuser",
            Password = "Test@123",
            Email = "admin@test.com",
            RepeatedPassword = "Test@123",
            Role = "Admin",
            SectionId = 1 // Invalid - admins should not have section ID
        };

        // Manually trigger validation context to simulate ModelState validation
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(registerDto);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(registerDto, validationContext, validationResults, true);

        foreach (var validationResult in validationResults)
        {
            _accountController.ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage ?? "Validation error");
        }

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenInstructorRegistrationWithSectionId()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "instructoruser",
            Password = "Test@123",
            Email = "instructor@test.com",
            RepeatedPassword = "Test@123",
            Role = "Instructor", // Instructor is alias for Teacher
            SectionId = 1 // Invalid - instructors should not have section ID
        };

        // Manually trigger validation context to simulate ModelState validation
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(registerDto);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(registerDto, validationContext, validationResults, true);

        foreach (var validationResult in validationResults)
        {
            _accountController.ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage ?? "Validation error");
        }

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
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

    #region Login Tests
    [Fact]
    public async Task Login_ReturnsOk_AndCorrectUsername_WhenLoginSuccessful()
    {
        // Arrange
        var loginDto = new LoginDto { Username = "test@test.com", Password = "Test@123" };
        var tokenResponseDto = new TokenResponseDto { AccessToken = "access_token", RefreshToken = "refresh_token" };
        _mockAccountService.Setup(s => s.LoginAsync(loginDto)).ReturnsAsync((tokenResponseDto, "testuser", "Student", null));

        // Act
        var result = await _accountController.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<LoginResponseDto>(okResult.Value);
        Assert.True(responseDto.Success);
        Assert.Equal("testuser", responseDto.User);
        Assert.Equal("Student", responseDto.Role);
    }
    #endregion
}
