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

    [Fact]
    public async Task GetSubjectByUuid_ReturnsOkResult_WhenSubjectExists()
    {
        var subjectUuid = Guid.NewGuid();
        var subject = new Subject { Id = 5, Uuid = subjectUuid, Name = "Physics", Code = "PHY101" };

        _mockSubjectService
            .Setup(s => s.GetSubjectByUuidAsync(subjectUuid))
            .ReturnsAsync(subject);

        var result = await _controller.GetSubjectByUuid(subjectUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSubject = Assert.IsType<Subject>(okResult.Value);
        Assert.Equal(subjectUuid, returnedSubject.Uuid);
        _mockSubjectService.Verify(s => s.GetSubjectByUuidAsync(subjectUuid), Times.Once);
    }

    [Fact]
    public async Task GetSubjectByUuid_ReturnsNotFound_WhenSubjectDoesNotExist()
    {
        var subjectUuid = Guid.NewGuid();

        _mockSubjectService
            .Setup(s => s.GetSubjectByUuidAsync(subjectUuid))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Subject", subjectUuid));

        var result = await _controller.GetSubjectByUuid(subjectUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
        _mockSubjectService.Verify(s => s.GetSubjectByUuidAsync(subjectUuid), Times.Once);
    }

    [Fact]
    public async Task UpdateSubjectByUuid_ReturnsOkResult_WhenUpdateSucceeds()
    {
        var subjectUuid = Guid.NewGuid();
        var updateSubject = new UpdateSubject { Name = "Advanced Physics" };
        var updatedSubject = new Subject { Id = 5, Uuid = subjectUuid, Name = updateSubject.Name! };

        _mockSubjectService
            .Setup(s => s.UpdateSubjectByUuidAsync(subjectUuid, updateSubject))
            .ReturnsAsync(updatedSubject);

        var result = await _controller.UpdateSubjectByUuid(subjectUuid, updateSubject);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSubject = Assert.IsType<Subject>(okResult.Value);
        Assert.Equal(subjectUuid, returnedSubject.Uuid);
    }

    [Fact]
    public async Task DeleteSubjectByUuid_ReturnsNoContent_WhenDeletionSucceeds()
    {
        var subjectUuid = Guid.NewGuid();

        _mockSubjectService
            .Setup(s => s.DeleteSubjectByUuidAsync(subjectUuid))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteSubjectByUuid(subjectUuid);

        Assert.IsType<NoContentResult>(result);
        _mockSubjectService.Verify(s => s.DeleteSubjectByUuidAsync(subjectUuid), Times.Once);
    }

    #endregion

    #region Dependency Check Tests

    [Fact]
    public async Task HasSchedulesInSubject_ReturnsOk_WithBooleanResult()
    {
        const int subjectId = 9;
        _mockSubjectService
            .Setup(service => service.HasSchedulesInSubjectAsync(subjectId))
            .ReturnsAsync(true);

        var result = await _controller.HasSchedulesInSubject(subjectId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
    }

    [Fact]
    public async Task HasEnrollmentsInSubject_ReturnsOk_WithBooleanResult()
    {
        const int subjectId = 4;
        _mockSubjectService
            .Setup(service => service.HasEnrollmentsInSubjectAsync(subjectId))
            .ReturnsAsync(true);

        var result = await _controller.HasEnrollmentsInSubject(subjectId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
    }

    [Fact]
    public async Task HasSchedulesInSubject_ReturnsBadRequest_ForInvalidId()
    {
        var result = await _controller.HasSchedulesInSubject(0);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Subject ID must be greater than 0.", badRequestResult.Value);
    }

    #endregion
}
