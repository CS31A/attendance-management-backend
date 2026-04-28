using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

/// <summary>
/// Test sa GetStudents endpoint
/// </summary>
public class StudentControllerTest
{
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly StudentController _studentController;

    public StudentControllerTest()
    {
        _mockStudentService = new Mock<IStudentService>();
        var mockLogger = new Mock<ILogger<StudentController>>();
        _studentController = new StudentController(_mockStudentService.Object, mockLogger.Object);
    }

    [Fact]
    public void StudentIdRoutes_AcceptGuidValues()
    {
        Assert.Equal("{id:guid}", GetHttpTemplate(nameof(StudentController.GetStudent)));
        Assert.Equal("{id:guid}", GetHttpTemplate(nameof(StudentController.PatchStudent)));
        Assert.Equal("{id:guid}/soft-delete", GetHttpTemplate(nameof(StudentController.SoftDeleteStudent)));
        Assert.Equal("{id:guid}", GetHttpTemplate(nameof(StudentController.HardDeleteStudent)));
        Assert.Equal("{id:guid}/restore", GetHttpTemplate(nameof(StudentController.RestoreStudent)));
    }

    private static string? GetHttpTemplate(string methodName)
    {
        var method = typeof(StudentController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        return method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .Single()
            .Template;
    }

    [Fact]
    public async Task GetStudents_ReturnsOkResult_WithStudentsList()
    {
        // Arrange
        var expectedStudents = new List<StudentListDto>
        {
            new StudentListDto { Id = Guid.NewGuid(), Firstname = "John", Lastname = "Doe" },
            new StudentListDto { Id = Guid.NewGuid(), Firstname = "Jane", Lastname = "Smith" }
        };

        _mockStudentService
            .Setup(s => s.GetAllNonDeletedStudentsAsync())
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _studentController.GetStudents();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IList<StudentListDto>>(okResult.Value);
        Assert.Equal(2, students.Count);
        Assert.Equal("John", students.First().Firstname);
        Assert.Equal("Smith", students.Last().Lastname);

        // Verify the service was called with correct parameters
        _mockStudentService.Verify(s => s.GetAllNonDeletedStudentsAsync(), Times.Once);
    }

    #region SearchStudentsByName Tests

    [Fact]
    public async Task SearchStudentsByName_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var searchTerm = "john";
        var expectedStudents = new List<StudentListDto>
        {
            new StudentListDto { Id = Guid.NewGuid(), Firstname = "John", Lastname = "Doe", UserId = "user1" },
            new StudentListDto { Id = Guid.NewGuid(), Firstname = "Johnny", Lastname = "Smith", UserId = "user2" }
        };

        _mockStudentService
            .Setup(s => s.SearchStudentsByNameAsync(searchTerm, 1, 50))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _studentController.SearchStudentsByName(searchTerm);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IEnumerable<StudentListDto>>(okResult.Value);
        Assert.Equal(2, students.Count());
        _mockStudentService.Verify(s => s.SearchStudentsByNameAsync(searchTerm, 1, 50), Times.Once);
    }

    [Fact]
    public async Task SearchStudentsByName_WithCustomPagination_ReturnsOkResult()
    {
        // Arrange
        var searchTerm = "doe";
        var pageNumber = 2;
        var pageSize = 20;
        var expectedStudents = new List<StudentListDto>
        {
            new StudentListDto { Id = Guid.NewGuid(), Firstname = "Jane", Lastname = "Doe", UserId = "user3" }
        };

        _mockStudentService
            .Setup(s => s.SearchStudentsByNameAsync(searchTerm, pageNumber, pageSize))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _studentController.SearchStudentsByName(searchTerm, pageNumber, pageSize);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IEnumerable<StudentListDto>>(okResult.Value);
        Assert.Single(students);
        _mockStudentService.Verify(s => s.SearchStudentsByNameAsync(searchTerm, pageNumber, pageSize), Times.Once);
    }

    [Fact]
    public async Task SearchStudentsByName_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        var searchTerm = "nonexistent";
        var expectedStudents = new List<StudentListDto>();

        _mockStudentService
            .Setup(s => s.SearchStudentsByNameAsync(searchTerm, 1, 50))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _studentController.SearchStudentsByName(searchTerm);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IEnumerable<StudentListDto>>(okResult.Value);
        Assert.Empty(students);
        _mockStudentService.Verify(s => s.SearchStudentsByNameAsync(searchTerm, 1, 50), Times.Once);
    }

    [Fact]
    public async Task SearchStudentsByName_WithNullQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _studentController.SearchStudentsByName(null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Query parameter is required", badRequestResult.Value);
        _mockStudentService.Verify(s => s.SearchStudentsByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SearchStudentsByName_WithEmptyQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _studentController.SearchStudentsByName("");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Query parameter is required", badRequestResult.Value);
        _mockStudentService.Verify(s => s.SearchStudentsByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SearchStudentsByName_WithWhitespaceQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _studentController.SearchStudentsByName("   ");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Query parameter is required", badRequestResult.Value);
        _mockStudentService.Verify(s => s.SearchStudentsByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SearchStudentsByName_WithServiceException_ReturnsBadRequest()
    {
        // Arrange
        var searchTerm = "te"; // Too short
        var errorMessage = "Search term must be at least 2 characters";

        _mockStudentService
            .Setup(s => s.SearchStudentsByNameAsync(searchTerm, 1, 50))
            .ThrowsAsync(new EntityServiceException("Student", "SearchStudentsByName", errorMessage));

        // Act
        var result = await _studentController.SearchStudentsByName(searchTerm);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(errorMessage, badRequestResult.Value);
    }

    #endregion

    #region SearchStudentsByEmail Tests

    [Fact]
    public async Task SearchStudentsByEmail_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var searchTerm = "@example.com";
        var expectedStudents = new List<StudentListDto>
        {
            new StudentListDto { Id = Guid.NewGuid(), Firstname = "John", Lastname = "Doe", UserId = "user1" },
            new StudentListDto { Id = Guid.NewGuid(), Firstname = "Jane", Lastname = "Smith", UserId = "user2" }
        };

        _mockStudentService
            .Setup(s => s.SearchStudentsByEmailAsync(searchTerm, 1, 50))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _studentController.SearchStudentsByEmail(searchTerm);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IEnumerable<StudentListDto>>(okResult.Value);
        Assert.Equal(2, students.Count());
        _mockStudentService.Verify(s => s.SearchStudentsByEmailAsync(searchTerm, 1, 50), Times.Once);
    }

    [Fact]
    public async Task SearchStudentsByEmail_WithCustomPagination_ReturnsOkResult()
    {
        // Arrange
        var searchTerm = "test";
        var pageNumber = 3;
        var pageSize = 10;
        var expectedStudents = new List<StudentListDto>
        {
            new StudentListDto { Id = Guid.NewGuid(), Firstname = "Test", Lastname = "User", UserId = "user5" }
        };

        _mockStudentService
            .Setup(s => s.SearchStudentsByEmailAsync(searchTerm, pageNumber, pageSize))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _studentController.SearchStudentsByEmail(searchTerm, pageNumber, pageSize);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IEnumerable<StudentListDto>>(okResult.Value);
        Assert.Single(students);
        _mockStudentService.Verify(s => s.SearchStudentsByEmailAsync(searchTerm, pageNumber, pageSize), Times.Once);
    }

    [Fact]
    public async Task SearchStudentsByEmail_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        var searchTerm = "nonexistent@domain.com";
        var expectedStudents = new List<StudentListDto>();

        _mockStudentService
            .Setup(s => s.SearchStudentsByEmailAsync(searchTerm, 1, 50))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _studentController.SearchStudentsByEmail(searchTerm);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IEnumerable<StudentListDto>>(okResult.Value);
        Assert.Empty(students);
        _mockStudentService.Verify(s => s.SearchStudentsByEmailAsync(searchTerm, 1, 50), Times.Once);
    }

    [Fact]
    public async Task SearchStudentsByEmail_WithNullQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _studentController.SearchStudentsByEmail(null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Query parameter is required", badRequestResult.Value);
        _mockStudentService.Verify(s => s.SearchStudentsByEmailAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SearchStudentsByEmail_WithEmptyQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _studentController.SearchStudentsByEmail("");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Query parameter is required", badRequestResult.Value);
        _mockStudentService.Verify(s => s.SearchStudentsByEmailAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SearchStudentsByEmail_WithWhitespaceQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _studentController.SearchStudentsByEmail("   ");

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Query parameter is required", badRequestResult.Value);
        _mockStudentService.Verify(s => s.SearchStudentsByEmailAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SearchStudentsByEmail_WithServiceException_ReturnsBadRequest()
    {
        // Arrange
        var searchTerm = "a"; // Too short
        var errorMessage = "Search term must be at least 2 characters";

        _mockStudentService
            .Setup(s => s.SearchStudentsByEmailAsync(searchTerm, 1, 50))
            .ThrowsAsync(new EntityServiceException("Student", "SearchStudentsByEmail", errorMessage));

        // Act
        var result = await _studentController.SearchStudentsByEmail(searchTerm);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(errorMessage, badRequestResult.Value);
    }

    #endregion
}
