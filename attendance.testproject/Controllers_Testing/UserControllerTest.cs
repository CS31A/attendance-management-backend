using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Exceptions;
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
                StudentProfile = new StudentProfileDto
                {
                    Id = 1,
                    Firstname = "John",
                    Lastname = "Doe",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            },
            new GetAllUsersDto
            {
                UserId = "user2",
                Username = "jane.smith",
                Email = "jane.smith@example.com",
                Role = "Instructor",
                InstructorProfile = new InstructorProfileDto
                {
                    Id = 2,
                    Firstname = "Jane",
                    Lastname = "Smith",
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                }
            },
            new GetAllUsersDto
            {
                UserId = "user3",
                Username = "admin.user",
                Email = "admin@example.com",
                Role = "Admin",
                AdminProfile = new AdminProfileDto
                {
                    Id = 1,
                    Firstname = "Admin",
                    Lastname = "User",
                    CreatedAt = DateTime.UtcNow.AddDays(-90),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        };

        _mockAccountService
            .Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
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
        Assert.NotNull(firstUser.StudentProfile);
        Assert.Equal(1, firstUser.StudentProfile.Id);
        Assert.Equal("John", firstUser.StudentProfile.Firstname);
        Assert.Equal("Doe", firstUser.StudentProfile.Lastname);

        // Verify service was called once with default Active status
        _mockAccountService.Verify(s => s.GetAllUsersAsync(UserStatus.Active), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOkResult_WithEmptyList_WhenNoUsers()
    {
        // Arrange
        var emptyUsersList = new List<GetAllUsersDto>();
        _mockAccountService
            .Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
            .ReturnsAsync(emptyUsersList);

        // Act
        var result = await _userController.GetAllUsers();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IList<GetAllUsersDto>>(okResult.Value);
        Assert.Empty(users);

        // Verify service was called once
        _mockAccountService.Verify(s => s.GetAllUsersAsync(UserStatus.Active), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsInternalServerError_WhenServiceThrowsException()
    {
        // Arrange
        var exceptionMessage = "Database connection failed";
        _mockAccountService
            .Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
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
        _mockAccountService.Verify(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_WithActiveStatus_ReturnsOnlyActiveUsers()
    {
        // Arrange
        var activeUsers = new List<GetAllUsersDto>
        {
            new GetAllUsersDto
            {
                UserId = "user1",
                Username = "john.doe",
                Email = "john.doe@example.com",
                Role = "Student",
                StudentProfile = new StudentProfileDto
                {
                    Id = 1,
                    Firstname = "John",
                    Lastname = "Doe",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        };

        _mockAccountService
            .Setup(s => s.GetAllUsersAsync(UserStatus.Active))
            .ReturnsAsync(activeUsers);

        // Act
        var result = await _userController.GetAllUsers(UserStatus.Active);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IList<GetAllUsersDto>>(okResult.Value);
        Assert.Single(users);
        Assert.Equal("john.doe", users.First().Username);

        // Verify service was called with Active status
        _mockAccountService.Verify(s => s.GetAllUsersAsync(UserStatus.Active), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_WithArchivedStatus_ReturnsOnlyArchivedUsers()
    {
        // Arrange
        var archivedUsers = new List<GetAllUsersDto>
        {
            new GetAllUsersDto
            {
                UserId = "user2",
                Username = "archived.user",
                Email = "archived@example.com",
                Role = "Student",
                StudentProfile = new StudentProfileDto
                {
                    Id = 2,
                    Firstname = "Archived",
                    Lastname = "User",
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                }
            }
        };

        _mockAccountService
            .Setup(s => s.GetAllUsersAsync(UserStatus.Archived))
            .ReturnsAsync(archivedUsers);

        // Act
        var result = await _userController.GetAllUsers(UserStatus.Archived);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IList<GetAllUsersDto>>(okResult.Value);
        Assert.Single(users);
        Assert.Equal("archived.user", users.First().Username);

        // Verify service was called with Archived status
        _mockAccountService.Verify(s => s.GetAllUsersAsync(UserStatus.Archived), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_WithAllStatus_ReturnsAllUsers()
    {
        // Arrange
        var allUsers = new List<GetAllUsersDto>
        {
            new GetAllUsersDto
            {
                UserId = "user1",
                Username = "active.user",
                Email = "active@example.com",
                Role = "Student",
                StudentProfile = new StudentProfileDto
                {
                    Id = 1,
                    Firstname = "Active",
                    Lastname = "User",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            },
            new GetAllUsersDto
            {
                UserId = "user2",
                Username = "archived.user",
                Email = "archived@example.com",
                Role = "Instructor",
                InstructorProfile = new InstructorProfileDto
                {
                    Id = 2,
                    Firstname = "Archived",
                    Lastname = "User",
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                }
            }
        };

        _mockAccountService
            .Setup(s => s.GetAllUsersAsync(UserStatus.All))
            .ReturnsAsync(allUsers);

        // Act
        var result = await _userController.GetAllUsers(UserStatus.All);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var users = Assert.IsAssignableFrom<IList<GetAllUsersDto>>(okResult.Value);
        Assert.Equal(2, users.Count);

        // Verify service was called with All status
        _mockAccountService.Verify(s => s.GetAllUsersAsync(UserStatus.All), Times.Once);
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
                StudentProfile = null // No profile data
            }
        };

        _mockAccountService
            .Setup(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()))
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
        Assert.Null(user.StudentProfile);
        Assert.Null(user.InstructorProfile);
        Assert.Null(user.AdminProfile);

        // Verify service was called once
        _mockAccountService.Verify(s => s.GetAllUsersAsync(It.IsAny<UserStatus>()), Times.Once);
    }

    #endregion

    #region SoftDeleteUser Tests

    [Fact]
    public async Task SoftDeleteUser_Student_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "student-user-id";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminDeleteUserAsync(adminId, targetUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("User deleted successfully", response.Message);

        // Verify service was called once with correct parameters
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteUser_Instructor_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "instructor-user-id";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminDeleteUserAsync(adminId, targetUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("User deleted successfully", response.Message);

        // Verify service was called once with correct parameters
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteUser_Admin_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "another-admin-user-id";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminDeleteUserAsync(adminId, targetUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("User deleted successfully", response.Message);

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
            .ThrowsAsync(new EntityNotFoundException<string>("User", targetUserId, expectedMessage));

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains(expectedMessage, response.Message);

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
            .ThrowsAsync(new ValidationException(expectedMessage));

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
            .ThrowsAsync(new EntityUnauthorizedException("User", "delete", expectedMessage));

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var forbiddenResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        var response = Assert.IsType<DeleteUserResponseDto>(forbiddenResult.Value);
        Assert.False(response.Success);

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
            .ThrowsAsync(new EntityNotFoundException<string>("User", targetUserId, expectedMessage));

        // Act
        var result = await _userController.SoftDeleteUser(targetUserId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains(expectedMessage, response.Message);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    #endregion

    #region HardDeleteUser Tests

    [Fact]
    public async Task HardDeleteUser_Student_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "student-user-id";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminHardDeleteUserAsync(adminId, targetUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userController.HardDeleteUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("User permanently deleted successfully", response.Message);

        // Verify service was called once with correct parameters
        _mockAccountService.Verify(s => s.AdminHardDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task HardDeleteUser_Instructor_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "instructor-user-id";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminHardDeleteUserAsync(adminId, targetUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userController.HardDeleteUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("User permanently deleted successfully", response.Message);

        // Verify service was called once with correct parameters
        _mockAccountService.Verify(s => s.AdminHardDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task HardDeleteUser_Admin_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "another-admin-user-id";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminHardDeleteUserAsync(adminId, targetUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userController.HardDeleteUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("User permanently deleted successfully", response.Message);

        // Verify service was called once with correct parameters
        _mockAccountService.Verify(s => s.AdminHardDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task HardDeleteUser_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "non-existent-user-id";
        var expectedMessage = "User not found";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminHardDeleteUserAsync(adminId, targetUserId))
            .ThrowsAsync(new EntityNotFoundException<string>("User", targetUserId, expectedMessage));

        // Act
        var result = await _userController.HardDeleteUser(targetUserId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains(expectedMessage, response.Message);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminHardDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task HardDeleteUser_SelfDeletion_ReturnsBadRequest()
    {
        // Arrange
        var adminId = "admin-user-id";
        var expectedMessage = "Cannot delete your own account";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminHardDeleteUserAsync(adminId, adminId))
            .ThrowsAsync(new ValidationException(expectedMessage));

        // Act
        var result = await _userController.HardDeleteUser(adminId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal(expectedMessage, response.Message);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminHardDeleteUserAsync(adminId, adminId), Times.Once);
    }

    [Fact]
    public async Task HardDeleteUser_Unauthorized_ReturnsUnauthorized()
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
        var result = await _userController.HardDeleteUser(targetUserId);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Admin not authenticated", response.Message);

        // Verify service was not called
        _mockAccountService.Verify(s => s.AdminHardDeleteUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HardDeleteUser_NotAdmin_ReturnsForbidden()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "user-id-to-delete";
        var expectedMessage = "Admin role required to delete users";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminHardDeleteUserAsync(adminId, targetUserId))
            .ThrowsAsync(new EntityUnauthorizedException("User", "hard delete", expectedMessage));

        // Act
        var result = await _userController.HardDeleteUser(targetUserId);

        // Assert
        var forbiddenResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        var response = Assert.IsType<DeleteUserResponseDto>(forbiddenResult.Value);
        Assert.False(response.Success);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminHardDeleteUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task HardDeleteUser_EmptyUserId_ReturnsBadRequest()
    {
        // Arrange
        var adminId = "admin-user-id";

        SetupControllerWithUser(adminId);

        // Act
        var result = await _userController.HardDeleteUser("");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("User ID is required", response.Message);

        // Verify service was not called
        _mockAccountService.Verify(s => s.AdminHardDeleteUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region RestoreUser Tests

    [Fact]
    public async Task RestoreUser_Student_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "student-user-id";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminRestoreUserAsync(adminId, targetUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userController.RestoreUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("User restored successfully", response.Message);

        // Verify service was called once with correct parameters
        _mockAccountService.Verify(s => s.AdminRestoreUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task RestoreUser_Instructor_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "instructor-user-id";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminRestoreUserAsync(adminId, targetUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userController.RestoreUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("User restored successfully", response.Message);

        // Verify service was called once with correct parameters
        _mockAccountService.Verify(s => s.AdminRestoreUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task RestoreUser_Admin_ReturnsOkResult_WithSuccessMessage()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "another-admin-user-id";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminRestoreUserAsync(adminId, targetUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userController.RestoreUser(targetUserId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("User restored successfully", response.Message);

        // Verify service was called once with correct parameters
        _mockAccountService.Verify(s => s.AdminRestoreUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task RestoreUser_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "non-existent-user-id";
        var expectedMessage = "User not found";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminRestoreUserAsync(adminId, targetUserId))
            .ThrowsAsync(new EntityNotFoundException<string>("User", targetUserId, expectedMessage));

        // Act
        var result = await _userController.RestoreUser(targetUserId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains(expectedMessage, response.Message);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminRestoreUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task RestoreUser_NotDeleted_ReturnsBadRequest()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "active-user-id";
        var expectedMessage = "Student profile not found or already active";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminRestoreUserAsync(adminId, targetUserId))
            .ThrowsAsync(new ValidationException(expectedMessage));

        // Act
        var result = await _userController.RestoreUser(targetUserId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal(expectedMessage, response.Message);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminRestoreUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task RestoreUser_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var targetUserId = "user-id-to-restore";

        // Setup controller without authenticated user
        _userController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal() // No claims
            }
        };

        // Act
        var result = await _userController.RestoreUser(targetUserId);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(unauthorizedResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Admin not authenticated", response.Message);

        // Verify service was not called
        _mockAccountService.Verify(s => s.AdminRestoreUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RestoreUser_NotAdmin_ReturnsForbidden()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "user-id-to-restore";
        var expectedMessage = "Unauthorized: Admin role required";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminRestoreUserAsync(adminId, targetUserId))
            .ThrowsAsync(new EntityUnauthorizedException("User", "restore", expectedMessage));

        // Act
        var result = await _userController.RestoreUser(targetUserId);

        // Assert
        var forbiddenResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbiddenResult.StatusCode);
        var response = Assert.IsType<DeleteUserResponseDto>(forbiddenResult.Value);
        Assert.False(response.Success);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminRestoreUserAsync(adminId, targetUserId), Times.Once);
    }

    [Fact]
    public async Task RestoreUser_EmptyUserId_ReturnsBadRequest()
    {
        // Arrange
        var adminId = "admin-user-id";

        SetupControllerWithUser(adminId);

        // Act
        var result = await _userController.RestoreUser("");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("User ID is required", response.Message);

        // Verify service was not called
        _mockAccountService.Verify(s => s.AdminRestoreUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RestoreUser_WhitespaceUserId_ReturnsBadRequest()
    {
        // Arrange
        var adminId = "admin-user-id";

        SetupControllerWithUser(adminId);

        // Act
        var result = await _userController.RestoreUser("   ");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("User ID is required", response.Message);

        // Verify service was not called
        _mockAccountService.Verify(s => s.AdminRestoreUserAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RestoreUser_AlreadyActive_ReturnsBadRequest()
    {
        // Arrange
        var adminId = "admin-user-id";
        var targetUserId = "already-active-user-id";
        var expectedMessage = "Instructor profile not found or already active";

        SetupControllerWithUser(adminId);

        _mockAccountService
            .Setup(s => s.AdminRestoreUserAsync(adminId, targetUserId))
            .ThrowsAsync(new ValidationException(expectedMessage));

        // Act
        var result = await _userController.RestoreUser(targetUserId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<DeleteUserResponseDto>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal(expectedMessage, response.Message);

        // Verify service was called once
        _mockAccountService.Verify(s => s.AdminRestoreUserAsync(adminId, targetUserId), Times.Once);
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
