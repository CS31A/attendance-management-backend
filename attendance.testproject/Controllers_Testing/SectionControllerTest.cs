using attendance_monitoring.Controllers;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
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
}
