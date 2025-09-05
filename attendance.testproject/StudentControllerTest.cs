using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.Request;

namespace attendance.testproject;

/// <summary>
/// Test sa GetStudents endpoint
/// </summary>
public class StudentControllerTest
{
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly StudentController _studentController;
    private readonly Mock<IStudentRepository> _mockStudentRepository;

    public StudentControllerTest()
    {
        _mockStudentService = new Mock<IStudentService>();
        _studentController = new StudentController(_mockStudentService.Object);
        _mockStudentRepository = new Mock<IStudentRepository>();
    }

    [Fact]
    public async Task GetStudents_ReturnsOkResult_WithStudentsList()
    {
        // Arrange
        var paginationQuery = new PaginationQuery { PageNumber = 1, PageSize = 10 };
        var expectedStudents = new List<Student>
        {
            new Student { Id = 1, Firstname = "John", Lastname = "Doe", Email = "john.doe@example.com" },
            new Student { Id = 2, Firstname = "Jane", Lastname = "Smith", Email = "jane.smith@example.com" }
        };

        _mockStudentService
            .Setup(s => s.GetAllStudentsAsync(It.IsAny<PaginationQuery>()))
            .ReturnsAsync(expectedStudents);

        _mockStudentRepository
            .Setup(x => x.GetAllStudentsAsync(It.IsAny<PaginationQuery>())).ReturnsAsync(expectedStudents);

        // Act
        var result = await _studentController.GetStudents(paginationQuery);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IEnumerable<Student>>(okResult.Value);
        Assert.Equal(2, students.Count());
        Assert.Equal("John", students.First().Firstname);
        Assert.Equal("jane.smith@example.com", students.Last().Email);
        
        // Verify the service was called with correct parameters
        _mockStudentService.Verify(s => s.GetAllStudentsAsync(paginationQuery), Times.Once);
    }
}
