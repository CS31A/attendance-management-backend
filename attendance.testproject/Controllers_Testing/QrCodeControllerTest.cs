using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.Controllers;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Controllers_Testing;

/// <summary>
/// Unit tests for QrCodeController
/// </summary>
public class QrCodeControllerTest
{
    private readonly Mock<ILogger<QrCodeController>> _mockLogger;
    private readonly QrCodeController _qrCodeController;

    public QrCodeControllerTest()
    {
        _mockLogger = new Mock<ILogger<QrCodeController>>();
        _qrCodeController = new QrCodeController(_mockLogger.Object);
    }

    [Fact]
    public async Task GenerateQrCode_WithValidRequest_ReturnsFileResult()
    {
        // Arrange
        var request = new QrCodeRequest
        {
            SectionId = 1,
            Schedule = 1001,
            RoomId = 101,
            SubjectName = "Mathematics",
            UniqueKey = "test-key-123"
        };

        // Act
        var result = await _qrCodeController.GenerateQrCode(request);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.NotNull(fileResult.FileContents);
        Assert.True(fileResult.FileContents.Length > 0);

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Generating QR code for section ID: {request.SectionId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Successfully generated QR code for section ID: {request.SectionId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    //[Fact]
    //public async Task GenerateQrCode_WithoutUniqueKey_GeneratesGuidAndReturnsFileResult()
    //{
    //    // Arrange
    //    var request = new QrCodeRequest
    //    {
    //        SectionId = 2,
    //        Schedule = 1002,
    //        RoomId = 202,
    //        SubjectName = "Physics",
    //        UniqueKey = null // No unique key provided
    //    };

    //    // Act
    //    var result = await _qrCodeController.GenerateQrCode(request);

    //    // Assert
    //    var fileResult = Assert.IsType<FileContentResult>(result);
    //    Assert.Equal("image/png", fileResult.ContentType);
    //    Assert.NotNull(fileResult.FileContents);
    //    Assert.True(fileResult.FileContents.Length > 0);

    //    // Verify that UniqueKey was generated
    //    Assert.False(string.IsNullOrEmpty(request.UniqueKey));
    //    Assert.True(Guid.TryParse(request.UniqueKey, out _));
    //}

    //[Fact]
    //public async Task GenerateQrCode_WithEmptyUniqueKey_GeneratesGuidAndReturnsFileResult()
    //{
    //    // Arrange
    //    var request = new QrCodeRequest
    //    {
    //        SectionId = 3,
    //        Schedule = 1003,
    //        RoomId = 303,
    //        SubjectName = "Chemistry",
    //        UniqueKey = "" // Empty unique key
    //    };

    //    // Act
    //    var result = await _qrCodeController.GenerateQrCode(request);

    //    // Assert
    //    var fileResult = Assert.IsType<FileContentResult>(result);
    //    Assert.NotNull(fileResult.FileContents);

    //    // Verify that UniqueKey was generated
    //    Assert.False(string.IsNullOrEmpty(request.UniqueKey));
    //    Assert.True(Guid.TryParse(request.UniqueKey, out _));
    //}

    [Fact]
    public async Task GenerateQrCode_WithMinimalData_ReturnsFileResult()
    {
        // Arrange
        var request = new QrCodeRequest
        {
            SectionId = 4,
            Schedule = 0,
            RoomId = 0,
            SubjectName = null,
            UniqueKey = null
        };

        // Act
        var result = await _qrCodeController.GenerateQrCode(request);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.NotNull(fileResult.FileContents);
        Assert.True(fileResult.FileContents.Length > 0);
    }

    [Fact]
    public async Task GenerateQrCode_LogsCorrectInformation()
    {
        // Arrange
        var request = new QrCodeRequest
        {
            SectionId = 7,
            Schedule = 1007,
            RoomId = 101,
            SubjectName = "English"
        };

        // Act
        await _qrCodeController.GenerateQrCode(request);

        // Assert - Verify both log messages were called
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Exactly(2)); // Once for start, once for success
    }
}