using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Controllers_Testing;

/// <summary>
/// Unit tests for QrCodeController
/// </summary>
public class QrCodeControllerTest
{
    private readonly Mock<IQrCodeService> _mockQrCodeService;
    private readonly Mock<ILogger<QrCodeController>> _mockLogger;
    private readonly QrCodeController _qrCodeController;

    public QrCodeControllerTest()
    {
        _mockQrCodeService = new Mock<IQrCodeService>();
        _mockLogger = new Mock<ILogger<QrCodeController>>();
        _qrCodeController = new QrCodeController(_mockQrCodeService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateQrCode_WithValidRequest_ReturnsFileResult()
    {
        // Arrange
        var request = new QrCodeRequest
        {
            SessionId = 1, // Now uses SessionId instead of ScheduleId/SectionId/ActualRoomId
            ExpirationMinutes = 30,
            UniqueHash = "test-unique-hash"
        };

        // Mock service response
        var mockResponse = new attendance_monitoring.Models.DTO.Response.QrCodeGenerationResponseDto
        {
            Success = true,
            QrHash = "test-hash-123",
            QrCodeId = 1,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };
        _mockQrCodeService.Setup(s => s.GenerateQrCodeAsync(It.IsAny<QrCodeRequest>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(mockResponse);

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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Generating QR code for session ID: {request.SessionId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully generated QR code with ID: {mockResponse.QrCodeId}, hash: {mockResponse.QrHash}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
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
            SessionId = 4, // Now uses SessionId
            ExpirationMinutes = 15,
            UniqueHash = "minimal-unique-hash"
        };

        // Mock service response
        var mockResponse = new attendance_monitoring.Models.DTO.Response.QrCodeGenerationResponseDto
        {
            Success = true,
            QrHash = "minimal-hash",
            QrCodeId = 4,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
        _mockQrCodeService.Setup(s => s.GenerateQrCodeAsync(It.IsAny<QrCodeRequest>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(mockResponse);

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
            SessionId = 7, // Now uses SessionId
            ExpirationMinutes = 60,
            UniqueHash = "log-test-unique-hash"
        };

        // Mock service response
        var mockResponse = new attendance_monitoring.Models.DTO.Response.QrCodeGenerationResponseDto
        {
            Success = true,
            QrHash = "log-test-hash",
            QrCodeId = 7,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };
        _mockQrCodeService.Setup(s => s.GenerateQrCodeAsync(It.IsAny<QrCodeRequest>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(mockResponse);

        // Act
        await _qrCodeController.GenerateQrCode(request);

        // Assert - Verify both log messages were called
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2)); // Once for start, once for success
    }
}