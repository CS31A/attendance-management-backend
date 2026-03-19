using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Exceptions;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class SubjectControllerTest
{
    private readonly Mock<ISubjectService> _mockSubjectService;
    private readonly Mock<ILogger<SubjectController>> _mockLogger;
    private readonly SubjectController _controller;

    public SubjectControllerTest()
    {
        _mockSubjectService = new Mock<ISubjectService>();
        _mockLogger = new Mock<ILogger<SubjectController>>();
        _controller = new SubjectController(_mockSubjectService.Object, _mockLogger.Object);
    }

    #region GetSubjects Tests

    [Fact]
    public async Task GetSubjects_ReturnsOkResult_WithSubjectsList()
    {
        // Arrange
        var expectedSubjects = new List<Subject>
        {
            new Subject { Id = 1, Name = "Mathematics" },
            new Subject { Id = 2, Name = "Science" }
        };
        _mockSubjectService
            .Setup(s => s.GetAllSubjectsAsync())
            .ReturnsAsync(expectedSubjects);

        // Act
        var result = await _controller.GetSubjects();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var subjects = Assert.IsAssignableFrom<IEnumerable<Subject>>(okResult.Value);
        Assert.Equal(2, subjects.Count());
        _mockSubjectService.Verify(s => s.GetAllSubjectsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSubjects_ThrowsException_WhenServiceThrowsException()
    {
        // Arrange
        _mockSubjectService
            .Setup(s => s.GetAllSubjectsAsync())
            .ThrowsAsync(new EntityServiceException("Subject", "GetAllSubjects", "Service error"));

        // Act & Assert
        // The controller no longer catches generic exceptions - they propagate to the global handler
        await Assert.ThrowsAsync<EntityServiceException>(() => _controller.GetSubjects());
    }

    #endregion
}