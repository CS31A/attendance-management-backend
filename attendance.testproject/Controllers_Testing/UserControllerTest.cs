using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;

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
}
