using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace attendance.testproject.Controllers_Testing;

/// <summary>
/// Unit tests for UserController
/// </summary>
public class UserControllerTest
{
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<ILogger<UserController>> _mockLogger;
    private readonly UserController _userController;

    public UserControllerTest()
    {
        _mockAccountService = new Mock<IAccountService>();
        _mockLogger = new Mock<ILogger<UserController>>();
        _userController = new UserController(_mockAccountService.Object, _mockLogger.Object);
    }

    #region GetAllUsers Tests

    [Fact]
    public async Task GetAllUsers_ReturnsOkResult_WithUsersList()
    {
        // Arrange
        var expectedUsers = new List<GetAllUsersDto>
        {
            new GetAllUsersDto
            {
                UserId = "user1",
                Username = "john.doe",
                Email = "john.doe@example.com",
                Role = "Student",
                ProfileId = 1,
                Firstname = "John",
                Lastname = "Doe",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new GetAllUsersDto
            {
                UserId = "user2",
                Username = "jane.smith",
                Email = "jane.smith@example.com",
                Role = "Instructor",
                ProfileId = 2,
                Firstname = "Jane",
                Lastname = "Smith",
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new GetAllUsersDto
            {
                UserId = "user3",
                Username = "admin.user",
                Email = "admin@example.com",
                Role = "Admin",
                ProfileId = 1,
                Firstname = "Admin",
                Lastname = "User",
                CreatedAt = DateTime.UtcNow.AddDays(-90),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };

        _mockAccountService
            .Setup(s => s.GetAllUsersAsync())
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _userController.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IList<GetAllUsersDto>>(okResult.Value);
        Assert.Equal(3, users.Count);
        
        // Verify first user details
        var firstUser = users.First();
        Assert.Equal("user1", firstUser.UserId);
        Assert.Equal("john.doe", firstUser.Username);
        Assert.Equal("john.doe@example.com", firstUser.Email);
        Assert.Equal("Student", firstUser.Role);
        Assert.Equal(1, firstUser.ProfileId);
        Assert.Equal("John", firstUser.Firstname);
        Assert.Equal("Doe", firstUser.Lastname);

        // Verify service was called once
        _mockAccountService.Verify(s => s.GetAllUsersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOkResult_WithEmptyList_WhenNoUsers()
    {
        // Arrange
        var emptyUsersList = new List<GetAllUsersDto>();
        _mockAccountService
            .Setup(s => s.GetAllUsersAsync())
            .ReturnsAsync(emptyUsersList);

        // Act
        var result = await _userController.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IList<GetAllUsersDto>>(okResult.Value);
        Assert.Empty(users);

        // Verify service was called once
        _mockAccountService.Verify(s => s.GetAllUsersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsInternalServerError_WhenServiceThrowsException()
    {
        // Arrange
        var exceptionMessage = "Database connection failed";
        _mockAccountService
            .Setup(s => s.GetAllUsersAsync())
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _userController.GetAllUsers();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        // Verify error response structure
        var errorResponse = statusCodeResult.Value;
        Assert.NotNull(errorResponse);
        
        // Use reflection to check anonymous object properties
        var successProperty = errorResponse.GetType().GetProperty("Success");
        var messageProperty = errorResponse.GetType().GetProperty("Message");
        
        Assert.NotNull(successProperty);
        Assert.NotNull(messageProperty);
        Assert.False((bool)successProperty.GetValue(errorResponse)!);
        Assert.Equal("An error occurred while retrieving users", messageProperty.GetValue(errorResponse));

        // Verify service was called once
        _mockAccountService.Verify(s => s.GetAllUsersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsUsersWithNullableFields_WhenProfileDataMissing()
    {
        // Arrange
        var usersWithNullFields = new List<GetAllUsersDto>
        {
            new GetAllUsersDto
            {
                UserId = "user1",
                Username = "incomplete.user",
                Email = "incomplete@example.com",
                Role = "Student",
                ProfileId = null,
                Firstname = null,
                Lastname = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockAccountService
            .Setup(s => s.GetAllUsersAsync())
            .ReturnsAsync(usersWithNullFields);

        // Act
        var result = await _userController.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IList<GetAllUsersDto>>(okResult.Value);
        Assert.Single(users);
        
        var user = users.First();
        Assert.Equal("user1", user.UserId);
        Assert.Equal("incomplete.user", user.Username);
        Assert.Equal("incomplete@example.com", user.Email);
        Assert.Equal("Student", user.Role);
        Assert.Null(user.ProfileId);
        Assert.Null(user.Firstname);
        Assert.Null(user.Lastname);

        // Verify service was called once
        _mockAccountService.Verify(s => s.GetAllUsersAsync(), Times.Once);
    }

    #endregion

    #region SoftDeleteUser Tests

    [Fact]
    public async Task SoftDeleteUser_Student_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "student-user-id";
        var expectedMessage = "Student profile deleted successfully";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminDeleteUserAsync(adminId, targetUserId))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedMessage, response.Message);

        // Verify service was called once with correct parameters
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteUser_Instructor_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "instructor-user-id";
        var expectedMessage = "Instructor profile deleted successfully";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminDeleteUserAsync(adminId, targetUserId))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedMessage, response.Message);

        // Verify service was called once with correct parameters
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteUser_Admin_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "another-admin-user-id";
        var expectedMessage = "Admin profile deleted successfully";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminDeleteUserAsync(adminId, targetUserId))
            .ReturnsAsync((true, expectedMessage));

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(expectedMessage, response.Message);

        // Verify service was called once with correct parameters
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteUser_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "non-existent-user-id";
        var expectedMessage = "User not found";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminDeleteUserAsync(adminId, targetUserId))
            .ReturnsAsync((false, expectedMessage));

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal(expectedMessage, response.Message);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteUser_SelfDeletion_ReturnsBadRequest()
    {
        // Arrange
        var adminId = "admin-user-id";
        var expectedMessage = "Cannot delete your own account";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminDeleteUserAsync(adminId, adminId))
            .ReturnsAsync((false, expectedMessage));

        // Act
        var result = await _userController.SoftDeleteUser(adminId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal(expectedMessage, response.Message);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(adminId, adminId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteUser_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var targetUserId = "user-id-to-delete";

        // Setup controller without authenticated user
        _userController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal() // No claims
            }
        };

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Admin not authenticated", response.Message);

        // Verify service was not called
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteUser_NotAdmin_ReturnsForbidden()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "user-id-to-delete";
        var expectedMessage = "Admin role required to delete users";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminDeleteUserAsync(adminId, targetUserId))
            .ReturnsAsync((false, expectedMessage));

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var forbiddenResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        var response = Assert.IsType<DeleteUserResponseDto>(forbiddenResult.Value);
        Assert.False(response.Success);
        Assert.Equal(expectedMessage, response.Message);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteUser_EmptyUserId_ReturnsBadRequest()
    {
        // Arrange
        var adminId = "admin-user-id";

        SetupControllerWithUser(adminId);

        // Act
        var result = await _userController.SoftDeleteUser("");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("User ID is required", response.Message);

        // Verify service was not called
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteUser_WhitespaceUserId_ReturnsBadRequest()
    {
        // Arrange
        var adminId = "admin-user-id";

        SetupControllerWithUser(adminId);

        // Act
        var result = await _userController.SoftDeleteUser("   ");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("User ID is required", response.Message);

        // Verify service was not called
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteUser_AlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "already-deleted-user-id";
        var expectedMessage = "Student profile not found or already deleted";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminDeleteUserAsync(adminId, targetUserId))
            .ReturnsAsync((false, expectedMessage));

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal(expectedMessage, response.Message);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupControllerWithUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _userController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #endregion
}
