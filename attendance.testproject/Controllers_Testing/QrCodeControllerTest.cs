using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.Controllers;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace attendance.testproject.Controllers_Testing;

/// <summary>
/// Unit tests for QrCodeController
/// </summary>
public class QrCodeControllerTest
{
    private readonly Mock<IQrCodeService> _mockQrCodeService;
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<ILogger<QrCodeController>> _mockLogger;
    private readonly Mock<UserContextService> _mockUserContextService;
    private readonly QrCodeController _qrCodeController;

    public QrCodeControllerTest()
    {
        _mockQrCodeService = new Mock<IQrCodeService>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockLogger = new Mock<ILogger<QrCodeController>>();
        
        var mockUserStore = new Mock<IUserStore<IdentityUser>>();
        var mockUserManager = new Mock<UserManager<IdentityUser>>(
            mockUserStore.Object,
            Options.Create(new IdentityOptions()),
            new Mock<IPasswordHasher<IdentityUser>>().Object,
            Array.Empty<IUserValidator<IdentityUser>>(),
            Array.Empty<IPasswordValidator<IdentityUser>>(),
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<IdentityUser>>>().Object);
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var mockContext = new Mock<ApplicationDbContext>(options);
        
        _mockUserContextService = new Mock<UserContextService>(mockUserManager.Object, mockContext.Object);
        _qrCodeController = new QrCodeController(_mockQrCodeService.Object, _mockSessionRepository.Object, _mockUserContextService.Object, _mockLogger.Object);
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
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Verify the response structure
        var response = okResult.Value;
        var responseType = response.GetType();
        var qrCodeImageProp = responseType.GetProperty("qrCodeImage");
        var qrCodeIdProp = responseType.GetProperty("qrCodeId");
        var qrHashProp = responseType.GetProperty("qrHash");
        var successProp = responseType.GetProperty("success");
        
        Assert.NotNull(qrCodeImageProp);
        var qrCodeImageValue = qrCodeImageProp.GetValue(response) as string;
        Assert.NotNull(qrCodeImageValue);
        Assert.True(qrCodeImageValue.Length > 0);
        
        // Verify it's valid base64
        var imageBytes = Convert.FromBase64String(qrCodeImageValue);
        Assert.True(imageBytes.Length > 0);
        
        // Verify other properties
        Assert.NotNull(qrCodeIdProp);
        Assert.Equal(1, qrCodeIdProp.GetValue(response));
        
        Assert.NotNull(qrHashProp);
        Assert.Equal("test-hash-123", qrHashProp.GetValue(response));
        
        Assert.NotNull(successProp);
        Assert.True((bool?)successProp.GetValue(response) ?? false);

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
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        // Verify the response structure
        var response = okResult.Value;
        var responseType = response.GetType();
        var qrCodeImageProp = responseType.GetProperty("qrCodeImage");
        var qrCodeIdProp = responseType.GetProperty("qrCodeId");
        var qrHashProp = responseType.GetProperty("qrHash");
        var successProp = responseType.GetProperty("success");
        
        Assert.NotNull(qrCodeImageProp);
        var qrCodeImageValue = qrCodeImageProp.GetValue(response) as string;
        Assert.NotNull(qrCodeImageValue);
        Assert.True(qrCodeImageValue.Length > 0);
        
        // Verify it's valid base64
        var imageBytes = Convert.FromBase64String(qrCodeImageValue);
        Assert.True(imageBytes.Length > 0);
        
        // Verify other properties
        Assert.NotNull(qrCodeIdProp);
        Assert.Equal(4, qrCodeIdProp.GetValue(response));
        
        Assert.NotNull(qrHashProp);
        Assert.Equal("minimal-hash", qrHashProp.GetValue(response));
        
        Assert.NotNull(successProp);
        Assert.True((bool?)successProp.GetValue(response) ?? false);
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

    [Fact]
    public async Task GetQrCodeImage_WithValidId_ReturnsFileResult()
    {
        // Arrange
        var qrCodeId = 1;
        var mockQrCode = new attendance_monitoring.Models.DTO.Response.QrCodeResponseDto
        {
            Id = qrCodeId,
            QrHash = "test-hash-123",
            SessionId = 1,
            GeneratedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsActive = true
        };

        _mockQrCodeService.Setup(s => s.GetQrCodeByIdAsync(qrCodeId))
            .ReturnsAsync(mockQrCode);

        // Act
        var result = await _qrCodeController.GetQrCodeImage(qrCodeId);

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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Retrieving QR code image for ID: {qrCodeId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Successfully generated image for QR code ID: {qrCodeId}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetQrCodeImage_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var qrCodeId = 999;
        _mockQrCodeService.Setup(s => s.GetQrCodeByIdAsync(qrCodeId))
            .ReturnsAsync((attendance_monitoring.Models.DTO.Response.QrCodeResponseDto?)null);

        // Act
        var result = await _qrCodeController.GetQrCodeImage(qrCodeId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);

        // Verify logging
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"QR code with ID {qrCodeId} not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetQrCodeImage_ReturnsValidPngImage()
    {
        // Arrange
        var qrCodeId = 5;
        var mockQrCode = new attendance_monitoring.Models.DTO.Response.QrCodeResponseDto
        {
            Id = qrCodeId,
            QrHash = "valid-hash-for-image-test",
            SessionId = 1,
            GeneratedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsActive = true
        };

        _mockQrCodeService.Setup(s => s.GetQrCodeByIdAsync(qrCodeId))
            .ReturnsAsync(mockQrCode);

        // Act
        var result = await _qrCodeController.GetQrCodeImage(qrCodeId);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        
        // Verify PNG header (PNG files start with specific bytes: 89 50 4E 47)
        Assert.True(fileResult.FileContents.Length > 8, "Image should have at least 8 bytes");
        Assert.Equal(0x89, fileResult.FileContents[0]); // PNG signature byte 1
        Assert.Equal(0x50, fileResult.FileContents[1]); // 'P'
        Assert.Equal(0x4E, fileResult.FileContents[2]); // 'N'
        Assert.Equal(0x47, fileResult.FileContents[3]); // 'G'
    }
}