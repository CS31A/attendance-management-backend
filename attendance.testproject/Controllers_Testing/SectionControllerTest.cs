using attendance_monitoring.Controllers;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class SectionControllerTest
{
    private readonly Mock<ISectionService> _mockSectionService;
    private readonly Mock<ILogger<SectionController>> _mockLogger;
    private readonly SectionController _controller;

    public SectionControllerTest()
    {
        _mockSectionService = new Mock<ISectionService>();
        _mockLogger = new Mock<ILogger<SectionController>>();
        _controller = new SectionController(_mockSectionService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task HasSchedulesInSection_ReturnsOk_WithBooleanResult()
    {
        // Arrange
        const int sectionId = 7;
        _mockSectionService
            .Setup(service => service.HasSchedulesInSectionAsync(sectionId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.HasSchedulesInSection(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
        _mockSectionService.Verify(service => service.HasSchedulesInSectionAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task HasSchedulesInSection_ReturnsBadRequest_ForInvalidId()
    {
        // Act
        var result = await _controller.HasSchedulesInSection(0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Section ID must be greater than 0.", badRequestResult.Value);
        _mockSectionService.Verify(service => service.HasSchedulesInSectionAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task HasSchedulesInSection_ReturnsServerError_WhenServiceThrowsEntityServiceException()
    {
        // Arrange
        const int sectionId = 12;
        _mockSectionService
            .Setup(service => service.HasSchedulesInSectionAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", $"HasSchedulesInSection: {sectionId}", "Error checking section dependencies"));

        // Act
        var result = await _controller.HasSchedulesInSection(sectionId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while checking section dependencies", objectResult.Value);
    }

    [Fact]
    public async Task DeleteSection_ReturnsNoContent_WhenDeletionSucceeds()
    {
        // Arrange
        const int sectionId = 1;
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockSectionService.Verify(service => service.DeleteSectionAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task DeleteSection_ReturnsNotFound_WhenSectionDoesNotExist()
    {
        // Arrange
        const int sectionId = 1;
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityNotFoundException<int>("Section", sectionId));

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Section with ID {sectionId} not found", notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteSection_ReturnsConflict_WithErrorResponseDto_WhenBlockedByDependencies()
    {
        // Arrange
        const int sectionId = 1;
        const string conflictMessage = "Cannot delete: Section has schedules assigned. Remove schedules first.";
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityConflictException("Section", "schedules", conflictMessage));

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(conflictResult.Value);
        Assert.False(errorResponse.Success);
        Assert.Equal(conflictMessage, errorResponse.Message);
        Assert.Equal(StatusCodes.Status409Conflict, errorResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteSection_ReturnsConflict_WithEnrollmentsMessage_WhenBlockedByEnrollments()
    {
        // Arrange
        const int sectionId = 1;
        const string conflictMessage = "Cannot delete: Section has student enrollments. Remove enrollments first.";
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityConflictException("Section", "enrollments", conflictMessage));

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(conflictResult.Value);
        Assert.Equal(conflictMessage, errorResponse.Message);
    }

    [Fact]
    public async Task DeleteSection_ReturnsConflict_WithStudentsMessage_WhenBlockedByStudents()
    {
        // Arrange
        const int sectionId = 1;
        const string conflictMessage = "Cannot delete: Section has assigned students. Reassign students first.";
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityConflictException("Section", "students", conflictMessage));

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(conflictResult.Value);
        Assert.Equal(conflictMessage, errorResponse.Message);
    }

    [Fact]
    public async Task DeleteSection_ReturnsServerError_ForUnexpectedServiceException()
    {
        // Arrange
        const int sectionId = 1;
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", $"DeleteSection: {sectionId}", "Database connection failed"));

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while deleting the section", objectResult.Value);
    }
}
