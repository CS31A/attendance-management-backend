using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class InstructorControllerTest
{
    private readonly Mock<IInstructorService> _mockInstructorService;
    private readonly Mock<ILogger<InstructorController>> _mockLogger;
    private readonly InstructorController _instructorController;

    public InstructorControllerTest()
    {
        _mockInstructorService = new Mock<IInstructorService>();
        _mockLogger = new Mock<ILogger<InstructorController>>();
        _instructorController = new InstructorController(_mockInstructorService.Object, _mockLogger.Object);

        var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "mock"));

        _instructorController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = mockUser }
        };
    }

    #region GetInstructors Tests

    [Fact]
    public async Task GetInstructors_ReturnsOkResult_WithInstructorsList()
    {
        // Arrange
        var expectedInstructors = new List<Instructor>
        {
            new Instructor { Id = 1, Firstname = "John", Lastname = "Doe" },
            new Instructor { Id = 2, Firstname = "Jane", Lastname = "Smith" }
        };
        _mockInstructorService
            .Setup(s => s.GetAllInstructorsAsync())
            .ReturnsAsync(expectedInstructors);

        // Act
        var result = await _instructorController.GetInstructors();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var instructors = Assert.IsAssignableFrom<IEnumerable<Instructor>>(okResult.Value);
        Assert.Equal(2, instructors.Count());
        _mockInstructorService.Verify(s => s.GetAllInstructorsAsync(), Times.Once);
    }

    #endregion
}