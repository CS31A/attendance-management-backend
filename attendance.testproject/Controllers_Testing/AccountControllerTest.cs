using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Exceptions;
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
        var registerDto = new RegisterDto { Username = "testuser", Password = "Test@123", Email = "test@test.com", RepeatedPassword = "Test@123", Role = "Student", SectionId = Guid.NewGuid() };
        var response = new RegisterResponseDto { Success = true, Message = "User registered successfully" };
        _mockAccountService.Setup(s => s.RegisterAsync(registerDto)).ReturnsAsync(response);

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
            SectionId = Guid.Empty // Invalid section ID
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
    public async Task Register_ReturnsOk_WhenInstructorRegistrationWithoutSectionId()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "instructoruser",
            Password = "Test@123",
            Email = "instructor@test.com",
            RepeatedPassword = "Test@123",
            Role = "Instructor"
        };
        var response = new RegisterResponseDto { Success = true, Message = "User registered successfully" };
        _mockAccountService.Setup(s => s.RegisterAsync(registerDto)).ReturnsAsync(response);

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<RegisterResponseDto>(okResult.Value);
        Assert.True(responseDto.Success);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenTeacherRoleProvided()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "teacheruser",
            Password = "Test@123",
            Email = "teacher@test.com",
            RepeatedPassword = "Test@123",
            Role = "Teacher"
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
        var response = new RegisterResponseDto { Success = true, Message = "User registered successfully" };
        _mockAccountService.Setup(s => s.RegisterAsync(registerDto)).ReturnsAsync(response);

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
            SectionId = Guid.NewGuid() // Invalid - admins should not have section ID
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
            Role = "Instructor",
            SectionId = Guid.NewGuid() // Invalid - instructors should not have section ID
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
    public async Task Register_PassesInstructorThrough_BeforeCallingService()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "instructoruser",
            Password = "Test@123",
            Email = "instructor@test.com",
            RepeatedPassword = "Test@123",
            Role = "Instructor"
        };

        RegisterDto? capturedDto = null;
        var response = new RegisterResponseDto { Success = true, Message = "User registered successfully" };

        _mockAccountService
            .Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
            .Callback<RegisterDto>(dto => capturedDto = dto) // Capture the DTO
            .ReturnsAsync(response);

        // Act
        await _accountController.Register(registerDto);

        // Assert
        Assert.NotNull(capturedDto);
        Assert.Equal("Instructor", capturedDto.Role);
        _mockAccountService.Verify(s => s.RegisterAsync(It.IsAny<RegisterDto>()), Times.Once);
    }
    [Fact]
    public async Task Register_ReturnsBadRequest_WhenTeacherRegistrationWithSectionId()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "teacher_redundant",
            Password = "Test@123",
            Email = "teacher_redundant@test.com",
            RepeatedPassword = "Test@123",
            Role = "Teacher",
            SectionId = Guid.NewGuid() // Should be rejected by DTO validation
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
        var response = Assert.IsType<RegisterResponseDto>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Invalid request data", response.Message);
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
        var loginResult = new LoginResult { TokenResponse = tokenResponseDto, Username = "testuser", Role = "Student" };
        _mockAccountService.Setup(s => s.LoginAsync(loginDto)).ReturnsAsync(loginResult);

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

    #region Register Exception Tests

    [Fact]
    public async Task Register_ReturnsConflict_WhenUsernameAlreadyExists()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "existinguser",
            Password = "Test@123",
            Email = "new@test.com",
            RepeatedPassword = "Test@123",
            Role = "Student",
            SectionId = Guid.NewGuid()
        };
        _mockAccountService.Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
            .ThrowsAsync(new EntityAlreadyExistsException<string>("User", "Username", "existinguser", "Username already exists"));

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var responseDto = Assert.IsType<RegisterResponseDto>(conflictResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task Register_ReturnsNotFound_WhenSectionNotFound()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Password = "Test@123",
            Email = "new@test.com",
            RepeatedPassword = "Test@123",
            Role = "Student",
            SectionId = Guid.NewGuid()
        };
        _mockAccountService.Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Section", registerDto.SectionId!.Value));

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var responseDto = Assert.IsType<RegisterResponseDto>(notFoundResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenValidationExceptionThrown()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Password = "weak",
            Email = "new@test.com",
            RepeatedPassword = "weak",
            Role = "Student",
            SectionId = Guid.NewGuid()
        };
        _mockAccountService.Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
            .ThrowsAsync(new ValidationException("Password does not meet requirements"));

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<RegisterResponseDto>(badRequestResult.Value);
        Assert.False(responseDto.Success);
        Assert.Contains("Password", responseDto.Message);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenEntityServiceExceptionThrown()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Password = "Test@123",
            Email = "new@test.com",
            RepeatedPassword = "Test@123",
            Role = "Student",
            SectionId = Guid.NewGuid()
        };
        _mockAccountService.Setup(s => s.RegisterAsync(It.IsAny<RegisterDto>()))
            .ThrowsAsync(new EntityServiceException("User", "register", "An error occurred while creating the user"));

        // Act
        var result = await _accountController.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<RegisterResponseDto>(badRequestResult.Value);
        Assert.False(responseDto.Success);
    }

    #endregion

    #region Login Exception Tests

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var loginDto = new LoginDto { Username = "", Password = "" };
        _accountController.ModelState.AddModelError("Username", "Required");

        // Act
        var result = await _accountController.Login(loginDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<LoginResponseDto>(badRequestResult.Value);
        Assert.False(responseDto.Success);
        Assert.Equal("Invalid request data", responseDto.Message);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsInvalid()
    {
        // Arrange
        var loginDto = new LoginDto { Username = "wronguser", Password = "wrongpassword" };
        _mockAccountService.Setup(s => s.LoginAsync(loginDto))
            .ThrowsAsync(new ValidationException("Invalid email or username or password"));

        // Act
        var result = await _accountController.Login(loginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var responseDto = Assert.IsType<LoginResponseDto>(unauthorizedResult.Value);
        Assert.False(responseDto.Success);
        Assert.Contains("Invalid", responseDto.Message);
    }

    #endregion

    #region WebLogin Tests

    [Fact]
    public async Task WebLogin_ReturnsOk_WhenLoginSuccessful()
    {
        // Arrange
        var webLoginDto = new WebLoginDto { Identifier = "test@test.com", Password = "Test@123" };
        var tokenResponseDto = new TokenResponseDto { AccessToken = "access_token", RefreshToken = "refresh_token" };
        var loginResult = new LoginResult { TokenResponse = tokenResponseDto, Username = "testuser", Role = "Student" };

        _mockAccountService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>())).ReturnsAsync(loginResult);

        // Setup mock HttpResponse for cookies
        var httpContext = new DefaultHttpContext();
        _accountController.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _accountController.WebLogin(webLoginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<WebLoginResponseDto>(okResult.Value);
        Assert.True(responseDto.Success);
        Assert.Equal("testuser", responseDto.Username);
        Assert.Equal("Student", responseDto.Role);
    }

    [Fact]
    public async Task WebLogin_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var webLoginDto = new WebLoginDto { Identifier = "", Password = "" };
        _accountController.ModelState.AddModelError("Identifier", "Required");

        // Act
        var result = await _accountController.WebLogin(webLoginDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<WebLoginResponseDto>(badRequestResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task WebLogin_ReturnsUnauthorized_WhenCredentialsInvalid()
    {
        // Arrange
        var webLoginDto = new WebLoginDto { Identifier = "wrong@test.com", Password = "wrongpassword" };
        _mockAccountService.Setup(s => s.LoginAsync(It.IsAny<LoginDto>()))
            .ThrowsAsync(new ValidationException("Invalid email or username or password"));

        var httpContext = new DefaultHttpContext();
        _accountController.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _accountController.WebLogin(webLoginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var responseDto = Assert.IsType<WebLoginResponseDto>(unauthorizedResult.Value);
        Assert.False(responseDto.Success);
    }

    #endregion

    #region Refresh Tests

    [Fact]
    public async Task Refresh_ReturnsOk_WhenRefreshSuccessful()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequestDto { RefreshToken = "valid_refresh_token" };
        var tokenResponse = new TokenResponseDto { AccessToken = "new_access_token", RefreshToken = "new_refresh_token" };
        _mockAccountService.Setup(s => s.RefreshAsync(refreshRequest)).ReturnsAsync(tokenResponse);

        // Act
        var result = await _accountController.Refresh(refreshRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<RefreshResponseDto>(okResult.Value);
        Assert.True(responseDto.Success);
        Assert.Equal("new_access_token", responseDto.AccessToken);
    }

    [Fact]
    public async Task Refresh_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequestDto { RefreshToken = "" };
        _accountController.ModelState.AddModelError("RefreshToken", "Required");

        // Act
        var result = await _accountController.Refresh(refreshRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<RefreshResponseDto>(badRequestResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task Refresh_ReturnsUnauthorized_WhenValidationExceptionThrown()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequestDto { RefreshToken = "expired_token" };
        _mockAccountService.Setup(s => s.RefreshAsync(refreshRequest))
            .ThrowsAsync(new ValidationException("Refresh token has expired"));

        // Act
        var result = await _accountController.Refresh(refreshRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var responseDto = Assert.IsType<RefreshResponseDto>(unauthorizedResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task Refresh_ReturnsUnauthorized_WhenTokenNotFound()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequestDto { RefreshToken = "nonexistent_token" };
        _mockAccountService.Setup(s => s.RefreshAsync(refreshRequest))
            .ThrowsAsync(new EntityNotFoundException<string>("RefreshToken", "nonexistent_token"));

        // Act
        var result = await _accountController.Refresh(refreshRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var responseDto = Assert.IsType<RefreshResponseDto>(unauthorizedResult.Value);
        Assert.False(responseDto.Success);
    }

    #endregion

    #region Revoke Tests

    [Fact]
    public async Task Revoke_ReturnsOk_WhenRevocationSuccessful()
    {
        // Arrange
        var revokeRequest = new RevokeTokenRequestDto { RefreshToken = "valid_refresh_token" };
        var revokeResponse = new RevokeResponseDto { Success = true, Message = "Token revoked successfully" };
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.RevokeAsync(revokeRequest, "user123")).ReturnsAsync(revokeResponse);

        // Act
        var result = await _accountController.Revoke(revokeRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<RevokeResponseDto>(okResult.Value);
        Assert.True(responseDto.Success);
    }

    [Fact]
    public async Task Revoke_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var revokeRequest = new RevokeTokenRequestDto { RefreshToken = "" };
        _accountController.ModelState.AddModelError("RefreshToken", "Required");

        // Act
        var result = await _accountController.Revoke(revokeRequest);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<RevokeResponseDto>(badRequestResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task Revoke_ReturnsUnauthorized_WhenUserNotFound()
    {
        // Arrange
        var revokeRequest = new RevokeTokenRequestDto { RefreshToken = "valid_token" };
        
        // No claims set - user not found
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await _accountController.Revoke(revokeRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var responseDto = Assert.IsType<RevokeResponseDto>(unauthorizedResult.Value);
        Assert.False(responseDto.Success);
        Assert.Equal("User not found", responseDto.Message);
    }

    [Fact]
    public async Task Revoke_ReturnsUnauthorized_WhenValidationExceptionThrown()
    {
        // Arrange
        var revokeRequest = new RevokeTokenRequestDto { RefreshToken = "invalid_token" };
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.RevokeAsync(revokeRequest, "user123"))
            .ThrowsAsync(new ValidationException("Token is invalid or already revoked"));

        // Act
        var result = await _accountController.Revoke(revokeRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var responseDto = Assert.IsType<RevokeResponseDto>(unauthorizedResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task Revoke_ReturnsUnauthorized_WhenEntityUnauthorizedExceptionThrown()
    {
        // Arrange
        var revokeRequest = new RevokeTokenRequestDto { RefreshToken = "another_users_token" };
        
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.RevokeAsync(revokeRequest, "user123"))
            .ThrowsAsync(new EntityUnauthorizedException("RefreshToken", "revoke", "user123", "Token does not belong to this user"));

        // Act
        var result = await _accountController.Revoke(revokeRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var responseDto = Assert.IsType<RevokeResponseDto>(unauthorizedResult.Value);
        Assert.False(responseDto.Success);
        Assert.Equal("Token does not belong to this user", responseDto.Message);
    }

    #endregion

    #region GetMe Tests

    [Fact]
    public async Task GetMe_ReturnsOk_WhenProfileFetchSuccessful()
    {
        // Arrange
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        var profileResponse = new UserProfileResponseDto
        {
            UserId = "user123",
            Username = "testuser",
            Email = "test@test.com",
            Role = "Student"
        };
        _mockAccountService.Setup(s => s.GetUserProfileAsync("user123")).ReturnsAsync(profileResponse);

        // Act
        var result = await _accountController.GetMe();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<UserProfileResponseDto>(okResult.Value);
        Assert.Equal("testuser", responseDto.Username);
    }

    [Fact]
    public async Task GetMe_ReturnsUnauthorized_WhenUserNotInClaims()
    {
        // Arrange - No claims set
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await _accountController.GetMe();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMe_ReturnsUnauthorized_WhenUserNotFound()
    {
        // Arrange
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "nonexistent_user") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.GetUserProfileAsync("nonexistent_user"))
            .ThrowsAsync(new EntityNotFoundException<string>("User", "nonexistent_user"));

        // Act
        var result = await _accountController.GetMe();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMe_ReturnsStudentProfile_WithCanonicalUuidIds()
    {
        // Arrange
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "student123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        var expectedProfileId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var expectedSectionId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var expectedCourseId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
        var profileResponse = new UserProfileResponseDto
        {
            UserId = "student123",
            Username = "studentuser",
            Email = "student@test.com",
            Role = "Student",
            StudentProfile = new StudentProfileInfo
            {
                Id = expectedProfileId,
                SectionId = expectedSectionId,
                CourseId = expectedCourseId,
                Firstname = "Alice",
                Lastname = "Student"
            }
        };
        _mockAccountService.Setup(s => s.GetUserProfileAsync("student123")).ReturnsAsync(profileResponse);

        // Act
        var result = await _accountController.GetMe();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<UserProfileResponseDto>(okResult.Value);
        Assert.NotNull(responseDto.StudentProfile);
        Assert.Equal(expectedProfileId, responseDto.StudentProfile!.Id);
        Assert.Equal(expectedSectionId, responseDto.StudentProfile.SectionId);
        Assert.Equal(expectedCourseId, responseDto.StudentProfile.CourseId);
    }

    [Fact]
    public async Task GetMe_ReturnsInstructorProfile_WithCanonicalUuidIds()
    {
        // Arrange
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "instructor123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        var expectedProfileId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
        var profileResponse = new UserProfileResponseDto
        {
            UserId = "instructor123",
            Username = "instructoruser",
            Email = "instructor@test.com",
            Role = "Instructor",
            InstructorProfile = new InstructorProfileInfo
            {
                Id = expectedProfileId,
                Firstname = "Ian",
                Lastname = "Instructor"
            }
        };
        _mockAccountService.Setup(s => s.GetUserProfileAsync("instructor123")).ReturnsAsync(profileResponse);

        // Act
        var result = await _accountController.GetMe();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<UserProfileResponseDto>(okResult.Value);
        Assert.NotNull(responseDto.InstructorProfile);
        Assert.Equal(expectedProfileId, responseDto.InstructorProfile!.Id);
    }

    [Fact]
    public async Task GetMe_ReturnsAdminProfile_WithCanonicalUuidIds()
    {
        // Arrange
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "admin123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        var expectedProfileId = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
        var profileResponse = new UserProfileResponseDto
        {
            UserId = "admin123",
            Username = "adminuser",
            Email = "admin@test.com",
            Role = "Admin",
            AdminProfile = new AdminProfileInfo
            {
                Id = expectedProfileId,
                Firstname = "Ada",
                Lastname = "Admin"
            }
        };
        _mockAccountService.Setup(s => s.GetUserProfileAsync("admin123")).ReturnsAsync(profileResponse);

        // Act
        var result = await _accountController.GetMe();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<UserProfileResponseDto>(okResult.Value);
        Assert.NotNull(responseDto.AdminProfile);
        Assert.Equal(expectedProfileId, responseDto.AdminProfile!.Id);
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public async Task UpdateProfile_ReturnsOk_WhenUpdateSuccessful()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile { Email = "newemail@test.com" };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        var profileResponse = new UserProfileResponseDto
        {
            UserId = "user123",
            Username = "testuser",
            Email = "newemail@test.com",
            Role = "Student"
        };
        _mockAccountService.Setup(s => s.UpdateUserProfileAsync("user123", updateProfileDto)).ReturnsAsync(profileResponse);

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(okResult.Value);
        Assert.True(responseDto.Success);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile();
        _accountController.ModelState.AddModelError("Email", "Invalid format");

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(badRequestResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsUnauthorized_WhenUserNotInClaims()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile { Email = "newemail@test.com" };
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(unauthorizedResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsNotFound_WhenUserNotFound()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile { Email = "newemail@test.com" };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.UpdateUserProfileAsync("user123", updateProfileDto))
            .ThrowsAsync(new EntityNotFoundException<string>("User", "user123"));

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(notFoundResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile { Email = "existing@test.com" };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.UpdateUserProfileAsync("user123", updateProfileDto))
            .ThrowsAsync(new EntityAlreadyExistsException<string>("User", "Email", "existing@test.com", "Email already in use"));

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(conflictResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsBadRequest_WhenValidationExceptionThrown()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile { Email = "invalid-email" };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.UpdateUserProfileAsync("user123", updateProfileDto))
            .ThrowsAsync(new ValidationException("Invalid email format"));

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(badRequestResult.Value);
        Assert.False(responseDto.Success);
    }

    #endregion

    #region UpdateProfile Password Change Tests

    [Fact]
    public async Task UpdateProfile_PasswordChange_ReturnsOk_WhenPasswordChangeSuccessful()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        var profileResponse = new UserProfileResponseDto
        {
            UserId = "user123",
            Username = "testuser",
            Email = "test@test.com",
            Role = "Student"
        };
        _mockAccountService.Setup(s => s.UpdateUserProfileAsync("user123", updateProfileDto)).ReturnsAsync(profileResponse);

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(okResult.Value);
        Assert.True(responseDto.Success);
        _mockAccountService.Verify(s => s.UpdateUserProfileAsync("user123", updateProfileDto), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_PasswordChange_ReturnsBadRequest_WhenCurrentPasswordIncorrect()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.UpdateUserProfileAsync("user123", updateProfileDto))
            .ThrowsAsync(new ValidationException("Current password is incorrect"));

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(badRequestResult.Value);
        Assert.False(responseDto.Success);
        Assert.Contains("Current password is incorrect", responseDto.Message);
    }

    [Fact]
    public async Task UpdateProfile_PasswordChange_ReturnsUnauthorized_WhenUserClaimMissing()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(unauthorizedResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task UpdateProfile_PasswordChange_ReturnsBadRequest_WhenPasswordTooShort()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "Short1!", // Less than 8 characters
            ConfirmNewPassword = "Short1!"
        };

        // Manually trigger validation context to simulate ModelState validation
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(updateProfileDto);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(updateProfileDto, validationContext, validationResults, true);

        foreach (var validationResult in validationResults)
        {
            _accountController.ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage ?? "Validation error");
        }

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(badRequestResult.Value);
        Assert.False(responseDto.Success);
        Assert.Equal("Invalid request data", responseDto.Message);
    }

    [Fact]
    public async Task UpdateProfile_PasswordChange_ReturnsBadRequest_WhenPasswordMismatch()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "DifferentPassword123!"
        };

        // Manually trigger validation context to simulate ModelState validation
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(updateProfileDto);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(updateProfileDto, validationContext, validationResults, true);

        foreach (var validationResult in validationResults)
        {
            _accountController.ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage ?? "Validation error");
        }

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(badRequestResult.Value);
        Assert.False(responseDto.Success);
        Assert.Equal("Invalid request data", responseDto.Message);
    }

    [Fact]
    public async Task UpdateProfile_PasswordChange_ReturnsBadRequest_WhenCurrentPasswordMissing()
    {
        // Arrange
        var updateProfileDto = new UpdateProfile
        {
            // CurrentPassword is intentionally missing
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.UpdateUserProfileAsync("user123", updateProfileDto))
            .ThrowsAsync(new ValidationException("Current password is required to change password"));

        // Act
        var result = await _accountController.UpdateProfile(updateProfileDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(badRequestResult.Value);
        Assert.False(responseDto.Success);
        Assert.Contains("Current password is required", responseDto.Message);
    }

    #endregion

    #region AdminUpdateUser Tests

    [Fact]
    public async Task AdminUpdateUser_ReturnsOk_WhenUpdateSuccessful()
    {
        // Arrange
        var targetUserId = "target123";
        var adminUpdateDto = new AdminUpdateUser { Email = "newemail@test.com" };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "admin123"), new(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        var profileResponse = new UserProfileResponseDto
        {
            UserId = targetUserId,
            Username = "targetuser",
            Email = "newemail@test.com",
            Role = "Student"
        };
        _mockAccountService.Setup(s => s.AdminUpdateUserProfileAsync("admin123", It.IsAny<AdminUpdateUser>())).ReturnsAsync(profileResponse);

        // Act
        var result = await _accountController.AdminUpdateUser(targetUserId, adminUpdateDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(okResult.Value);
        Assert.True(responseDto.Success);
    }

    [Fact]
    public async Task AdminUpdateUser_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var adminUpdateDto = new AdminUpdateUser();
        _accountController.ModelState.AddModelError("Email", "Invalid format");

        // Act
        var result = await _accountController.AdminUpdateUser("target123", adminUpdateDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(badRequestResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task AdminUpdateUser_ReturnsUnauthorized_WhenAdminNotInClaims()
    {
        // Arrange
        var adminUpdateDto = new AdminUpdateUser { Email = "newemail@test.com" };
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await _accountController.AdminUpdateUser("target123", adminUpdateDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(unauthorizedResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task AdminUpdateUser_ReturnsNotFound_WhenTargetUserNotFound()
    {
        // Arrange
        var targetUserId = "nonexistent123";
        var adminUpdateDto = new AdminUpdateUser { Email = "newemail@test.com" };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "admin123"), new(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.AdminUpdateUserProfileAsync("admin123", It.IsAny<AdminUpdateUser>()))
            .ThrowsAsync(new EntityNotFoundException<string>("User", targetUserId));

        // Act
        var result = await _accountController.AdminUpdateUser(targetUserId, adminUpdateDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(notFoundResult.Value);
        Assert.False(responseDto.Success);
    }

    [Fact]
    public async Task AdminUpdateUser_ReturnsForbidden_WhenUnauthorizedException()
    {
        // Arrange
        var targetUserId = "target123";
        var adminUpdateDto = new AdminUpdateUser { Email = "newemail@test.com" };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "admin123"), new(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.AdminUpdateUserProfileAsync("admin123", It.IsAny<AdminUpdateUser>()))
            .ThrowsAsync(new EntityUnauthorizedException("User", "update", "admin123", "Cannot update this user"));

        // Act
        var result = await _accountController.AdminUpdateUser(targetUserId, adminUpdateDto);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, statusResult.StatusCode);
        var response = Assert.IsType<UpdateProfileResponse>(statusResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Cannot update this user", response.Message);
    }

    [Fact]
    public async Task AdminUpdateUser_ReturnsConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        var targetUserId = "target123";
        var adminUpdateDto = new AdminUpdateUser { Email = "existing@test.com" };
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "admin123"), new(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };

        _mockAccountService.Setup(s => s.AdminUpdateUserProfileAsync("admin123", It.IsAny<AdminUpdateUser>()))
            .ThrowsAsync(new EntityAlreadyExistsException<string>("User", "Email", "existing@test.com", "Email already in use"));

        // Act
        var result = await _accountController.AdminUpdateUser(targetUserId, adminUpdateDto);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var responseDto = Assert.IsType<UpdateProfileResponse>(conflictResult.Value);
        Assert.False(responseDto.Success);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ReturnsOk_WhenLogoutSuccessful()
    {
        // Arrange
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        httpContext.Request.Headers["Authorization"] = "Bearer test_token";
        _accountController.ControllerContext = new ControllerContext { HttpContext = httpContext };

        _mockAccountService.Setup(s => s.LogoutAsync("user123", "test_token"))
            .ReturnsAsync(new LogoutResponseDto { Success = true, Message = "Logged out successfully" });

        // Act
        var result = await _accountController.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<LogoutResponseDto>(okResult.Value);
        Assert.True(responseDto.Success);
    }

    [Fact]
    public async Task Logout_ReturnsOk_WhenUserNotInClaims()
    {
        // Arrange - No claims (prevents timing attacks by always returning success)
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await _accountController.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<LogoutResponseDto>(okResult.Value);
        Assert.True(responseDto.Success); // Always returns success to prevent timing attacks
    }

    #endregion

    #region WebLogout Tests

    [Fact]
    public async Task WebLogout_ReturnsOk_WhenLogoutSuccessful()
    {
        // Arrange
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user123") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        httpContext.Request.Headers.Cookie = "accessToken=test_token; refreshToken=test_refresh";
        _accountController.ControllerContext = new ControllerContext { HttpContext = httpContext };

        _mockAccountService.Setup(s => s.WebLogoutAsync("user123", It.IsAny<string?>()))
            .ReturnsAsync(new LogoutResponseDto { Success = true, Message = "Logged out successfully" });

        // Act
        var result = await _accountController.WebLogout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<LogoutResponseDto>(okResult.Value);
        Assert.True(responseDto.Success);
    }

    [Fact]
    public async Task WebLogout_ReturnsOk_WhenUserNotInClaims()
    {
        // Arrange - No claims (prevents timing attacks by always returning success)
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await _accountController.WebLogout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseDto = Assert.IsType<LogoutResponseDto>(okResult.Value);
        Assert.True(responseDto.Success); // Always returns success to prevent timing attacks
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(string userId, string? username = null, string? role = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        if (!string.IsNullOrEmpty(username))
            claims.Add(new Claim(ClaimTypes.Name, username));
        if (!string.IsNullOrEmpty(role))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _accountController.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = claimsPrincipal } };
    }

    #endregion
}
