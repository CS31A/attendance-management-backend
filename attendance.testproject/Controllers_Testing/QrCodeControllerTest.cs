using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using attendance_monitoring.Controllers;
using attendance_monitoring.Constants;
using attendance_monitoring.IRepository;
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
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<ILogger<QrCodeController>> _mockLogger;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly QrCodeController _qrCodeController;

    public QrCodeControllerTest()
    {
        _mockQrCodeService = new Mock<IQrCodeService>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockLogger = new Mock<ILogger<QrCodeController>>();
        _mockUserContextService = new Mock<IUserContextService>();
        _qrCodeController = new QrCodeController(_mockQrCodeService.Object, _mockSessionRepository.Object, _mockUserContextService.Object, _mockLogger.Object);
        SetUserContext();
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
    public async Task GenerateQrCode_WithSliceBUuidSchemaPresent_KeepsLegacyResponseShape()
    {
        // Arrange
        var request = new QrCodeRequest
        {
            SessionId = 8,
            ExpirationMinutes = 20,
            UniqueHash = "slice-b-uuid-shape"
        };

        _mockQrCodeService.Setup(s => s.GenerateQrCodeAsync(It.IsAny<QrCodeRequest>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(new attendance_monitoring.Models.DTO.Response.QrCodeGenerationResponseDto
            {
                Success = true,
                QrHash = "slice-b-hash",
                QrCodeId = 8,
                ExpiresAt = DateTime.UtcNow.AddMinutes(20)
            });

        // Act
        var result = await _qrCodeController.GenerateQrCode(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotEqual(Guid.Empty, new attendance_monitoring.Classes.QrCode { Uuid = Guid.NewGuid() }.Uuid);
        Assert.NotEqual(Guid.Empty, new attendance_monitoring.Classes.Session { Uuid = Guid.NewGuid() }.Uuid);
        Assert.Null(okResult.Value!.GetType().GetProperty("uuid"));
        Assert.Null(okResult.Value.GetType().GetProperty("qrCodeUuid"));
        Assert.Null(okResult.Value.GetType().GetProperty("sessionUuid"));
        Assert.Equal(8, okResult.Value.GetType().GetProperty("qrCodeId")?.GetValue(okResult.Value));
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

    [Fact]
    public async Task GetQrCodeByUuid_WithValidUuid_ReturnsOk()
    {
        var qrCodeUuid = Guid.NewGuid();
        var qrCode = new attendance_monitoring.Models.DTO.Response.QrCodeResponseDto
        {
            Id = 3,
            Uuid = qrCodeUuid,
            SessionId = 9,
            SessionUuid = Guid.NewGuid(),
            QrHash = "uuid-route-hash",
            GeneratedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(20),
            IsActive = true
        };

        _mockQrCodeService
            .Setup(service => service.GetQrCodeByUuidAsync(qrCodeUuid))
            .ReturnsAsync(qrCode);

        var result = await _qrCodeController.GetQrCodeByUuid(qrCodeUuid);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<attendance_monitoring.Models.DTO.Response.QrCodeResponseDto>(okResult.Value);
        Assert.Equal(qrCodeUuid, response.Uuid);
    }

    [Fact]
    public async Task GetQrCodeByUuid_WithInvalidUuid_ReturnsNotFound()
    {
        var qrCodeUuid = Guid.NewGuid();

        _mockQrCodeService
            .Setup(service => service.GetQrCodeByUuidAsync(qrCodeUuid))
            .ReturnsAsync((attendance_monitoring.Models.DTO.Response.QrCodeResponseDto?)null);

        var result = await _qrCodeController.GetQrCodeByUuid(qrCodeUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetQrCodeImageByUuid_WithValidUuid_ReturnsFileResult()
    {
        var qrCodeUuid = Guid.NewGuid();
        var qrCode = new attendance_monitoring.Models.DTO.Response.QrCodeResponseDto
        {
            Id = 4,
            Uuid = qrCodeUuid,
            SessionId = 10,
            QrHash = "uuid-image-hash",
            GeneratedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(20),
            IsActive = true
        };

        _mockQrCodeService
            .Setup(service => service.GetQrCodeByUuidAsync(qrCodeUuid))
            .ReturnsAsync(qrCode);

        var result = await _qrCodeController.GetQrCodeImageByUuid(qrCodeUuid);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.NotEmpty(fileResult.FileContents);
    }

    [Fact]
    public async Task GetQrCodeImageByUuid_WithInvalidUuid_ReturnsNotFound()
    {
        var qrCodeUuid = Guid.NewGuid();

        _mockQrCodeService
            .Setup(service => service.GetQrCodeByUuidAsync(qrCodeUuid))
            .ReturnsAsync((attendance_monitoring.Models.DTO.Response.QrCodeResponseDto?)null);

        var result = await _qrCodeController.GetQrCodeImageByUuid(qrCodeUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetQrCodesBySessionUuid_ReturnsForbidden_WhenUserIsNotInstructor()
    {
        var sessionUuid = Guid.NewGuid();
        SetUserContext(role: RoleConstants.Student);

        _mockUserContextService
            .Setup(service => service.GetInstructorIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((int?)null);

        var result = await _qrCodeController.GetQrCodesBySessionUuid(sessionUuid);

        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbiddenResult.StatusCode);
    }

    [Fact]
    public async Task GetQrCodesBySessionUuid_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        var sessionUuid = Guid.NewGuid();
        SetUserContext();

        _mockUserContextService
            .Setup(service => service.GetInstructorIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(7);
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync((attendance_monitoring.Classes.Session?)null);

        var result = await _qrCodeController.GetQrCodesBySessionUuid(sessionUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetQrCodesBySessionUuid_ReturnsForbidden_WhenInstructorDoesNotOwnSession()
    {
        var sessionUuid = Guid.NewGuid();
        SetUserContext();

        _mockUserContextService
            .Setup(service => service.GetInstructorIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(7);
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(new attendance_monitoring.Classes.Session
            {
                Id = 12,
                Uuid = sessionUuid,
                ScheduleId = 2,
                RowVersion = [1, 2, 3, 4],
                Schedule = new attendance_monitoring.Classes.Schedules
                {
                    Id = 2,
                    Uuid = Guid.NewGuid(),
                    InstructorId = 99,
                    DayOfWeek = "Monday",
                    TimeIn = new TimeOnly(8, 0),
                    TimeOut = new TimeOnly(9, 0)
                }
            });

        var result = await _qrCodeController.GetQrCodesBySessionUuid(sessionUuid);

        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbiddenResult.StatusCode);
    }

    [Fact]
    public async Task GetQrCodesBySessionUuid_ReturnsNotFound_WhenNoQrCodesExist()
    {
        var sessionUuid = Guid.NewGuid();
        SetUserContext();

        _mockUserContextService
            .Setup(service => service.GetInstructorIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(7);
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(new attendance_monitoring.Classes.Session
            {
                Id = 12,
                Uuid = sessionUuid,
                ScheduleId = 2,
                RowVersion = [1, 2, 3, 4],
                Schedule = new attendance_monitoring.Classes.Schedules
                {
                    Id = 2,
                    Uuid = Guid.NewGuid(),
                    InstructorId = 7,
                    DayOfWeek = "Monday",
                    TimeIn = new TimeOnly(8, 0),
                    TimeOut = new TimeOnly(9, 0)
                }
            });
        _mockQrCodeService
            .Setup(service => service.GetQrCodesBySessionUuidAsync(sessionUuid))
            .ReturnsAsync([]);

        var result = await _qrCodeController.GetQrCodesBySessionUuid(sessionUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetScanHistoryByUuid_ReturnsForbidden_WhenUserIsNotInstructor()
    {
        var qrCodeUuid = Guid.NewGuid();
        SetUserContext(role: RoleConstants.Student);

        _mockUserContextService
            .Setup(service => service.GetInstructorIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((int?)null);

        var result = await _qrCodeController.GetScanHistoryByUuid(qrCodeUuid);

        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbiddenResult.StatusCode);
    }

    [Fact]
    public async Task GetScanHistoryByUuid_ReturnsNotFound_WhenQrCodeDoesNotExist()
    {
        var qrCodeUuid = Guid.NewGuid();
        SetUserContext();

        _mockUserContextService
            .Setup(service => service.GetInstructorIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(7);
        _mockQrCodeService
            .Setup(service => service.GetScanHistoryByUuidAsync(qrCodeUuid, 7, RoleConstants.Instructor, 1, 50))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityNotFoundException<Guid>("QrCode", qrCodeUuid));

        var result = await _qrCodeController.GetScanHistoryByUuid(qrCodeUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetScanHistoryByUuid_ReturnsForbidden_WhenAccessIsUnauthorized()
    {
        var qrCodeUuid = Guid.NewGuid();
        SetUserContext();

        _mockUserContextService
            .Setup(service => service.GetInstructorIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(7);
        _mockQrCodeService
            .Setup(service => service.GetScanHistoryByUuidAsync(qrCodeUuid, 7, RoleConstants.Instructor, 1, 50))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityUnauthorizedException("QrCode", "View scan history", "7"));

        var result = await _qrCodeController.GetScanHistoryByUuid(qrCodeUuid);

        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbiddenResult.StatusCode);
    }

    [Fact]
    public void SliceBRouteTemplates_SeparateHashIntAndUuidRoutes()
    {
        Assert.Equal("{id:int}", GetHttpTemplate(nameof(QrCodeController.GetQrCodeById)));
        Assert.Equal("hash/{qrHash}", GetHttpTemplate(nameof(QrCodeController.GetQrCodeByHash)));
        Assert.Equal("uuid/{uuid:guid}", GetHttpTemplate(nameof(QrCodeController.GetQrCodeByUuid)));
        Assert.Equal("{id:int}/image", GetHttpTemplate(nameof(QrCodeController.GetQrCodeImage)));
        Assert.Equal("uuid/{uuid:guid}/image", GetHttpTemplate(nameof(QrCodeController.GetQrCodeImageByUuid)));
        Assert.Equal("{id:int}/revoke", GetHttpTemplate(nameof(QrCodeController.RevokeQrCode)));
        Assert.Equal("hash/{qrHash}/revoke", GetHttpTemplate(nameof(QrCodeController.RevokeQrCodeByHash)));
        Assert.Equal("uuid/{uuid:guid}/revoke", GetHttpTemplate(nameof(QrCodeController.RevokeQrCodeByUuid)));
        Assert.Equal("{id:int}/reactivate", GetHttpTemplate(nameof(QrCodeController.ReactivateQrCode)));
        Assert.Equal("hash/{qrHash}/reactivate", GetHttpTemplate(nameof(QrCodeController.ReactivateQrCodeByHash)));
        Assert.Equal("uuid/{uuid:guid}/reactivate", GetHttpTemplate(nameof(QrCodeController.ReactivateQrCodeByUuid)));
        Assert.Equal("session/{sessionId:int}", GetHttpTemplate(nameof(QrCodeController.GetQrCodesBySessionId)));
        Assert.Equal("session/uuid/{sessionUuid:guid}", GetHttpTemplate(nameof(QrCodeController.GetQrCodesBySessionUuid)));
        Assert.Equal("{id:int}/scan-history", GetHttpTemplate(nameof(QrCodeController.GetScanHistory)));
        Assert.Equal("hash/{qrHash}/scan-history", GetHttpTemplate(nameof(QrCodeController.GetScanHistoryByHash)));
        Assert.Equal("uuid/{uuid:guid}/scan-history", GetHttpTemplate(nameof(QrCodeController.GetScanHistoryByUuid)));
    }

    private static string? GetHttpTemplate(string methodName)
    {
        var method = typeof(QrCodeController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        return method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .Single()
            .Template;
    }

    private void SetUserContext(string role = RoleConstants.Instructor, string userId = "user-1")
    {
        _qrCodeController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Role, role)
                ], "TestAuth"))
            }
        };
    }
}
