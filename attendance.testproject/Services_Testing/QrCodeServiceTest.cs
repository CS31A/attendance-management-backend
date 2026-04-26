using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Extensions;
using attendance_monitoring.Options;
using attendance_monitoring.Services;
using attendance_monitoring.Services.QrCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace attendance.testproject.Services_Testing;

public class QrCodeServiceTest
{
    [Fact]
    public async Task GetQrCodeByUuidAsync_DelegatesToQueryService()
    {
        var qrCodeUuid = Guid.NewGuid();
        var qrCodeRepository = new Mock<IQrCodeRepository>();
        qrCodeRepository
            .Setup(repository => repository.GetQrCodeByUuidAsync(qrCodeUuid))
            .ReturnsAsync(CreateQrCode(uuid: qrCodeUuid));

        await using var context = CreateDbContext();
        var service = CreateFacade(
            queryService: new QrCodeQueryService(qrCodeRepository.Object, context, NullLogger<QrCodeQueryService>.Instance));

        var response = await service.GetQrCodeByUuidAsync(qrCodeUuid);

        Assert.NotNull(response);
        Assert.Equal(qrCodeUuid, response.Id);
        qrCodeRepository.Verify(repository => repository.GetQrCodeByUuidAsync(qrCodeUuid), Times.Once);
    }

    [Fact]
    public async Task UpdateQrCodeByUuidAsync_DelegatesToWriteService()
    {
        var qrCode = CreateQrCode();
        var qrCodeRepository = CreateAuthorizedRepository();
        qrCodeRepository
            .Setup(repository => repository.GetQrCodeByUuidAsync(qrCode.Id))
            .ReturnsAsync(qrCode);
        qrCodeRepository
            .Setup(repository => repository.UpdateQrCodeAsync(It.IsAny<QrCode>()))
            .ReturnsAsync((QrCode updated) => updated);

        var service = CreateFacade(writeService: CreateWriteService(qrCodeRepository.Object));

        var response = await service.UpdateQrCodeByUuidAsync(qrCode.Id, new UpdateQrCode { IsActive = false }, CreateUser());

        Assert.NotNull(response);
        Assert.Equal(qrCode.Id, response.Id);
        qrCodeRepository.Verify(repository => repository.GetQrCodeByUuidAsync(qrCode.Id), Times.Once);
        qrCodeRepository.Verify(repository => repository.UpdateQrCodeAsync(It.Is<QrCode>(entity => entity.Id == qrCode.Id && entity.IsActive == false)), Times.Once);
        qrCodeRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RevokeQrCodeByUuidAsync_DelegatesToWriteService()
    {
        var qrCode = CreateQrCode();
        var qrCodeRepository = CreateAuthorizedRepository();
        qrCodeRepository
            .Setup(repository => repository.GetQrCodeByUuidAsync(qrCode.Id))
            .ReturnsAsync(qrCode);
        qrCodeRepository
            .Setup(repository => repository.UpdateQrCodeAsync(It.IsAny<QrCode>()))
            .ReturnsAsync((QrCode updated) => updated);

        var service = CreateFacade(writeService: CreateWriteService(qrCodeRepository.Object));

        await service.RevokeQrCodeByUuidAsync(qrCode.Id, "rotated", CreateUser());

        qrCodeRepository.Verify(repository => repository.GetQrCodeByUuidAsync(qrCode.Id), Times.Once);
        qrCodeRepository.Verify(repository => repository.UpdateQrCodeAsync(It.Is<QrCode>(entity => entity.Id == qrCode.Id && entity.IsActive == false && entity.RevocationReason == "rotated")), Times.Once);
        qrCodeRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReactivateQrCodeByUuidAsync_DelegatesToWriteService()
    {
        var qrCode = CreateQrCode(expiresAt: DateTime.UtcNow.AddMinutes(10), isActive: false);
        var qrCodeRepository = CreateAuthorizedRepository();
        qrCodeRepository
            .Setup(repository => repository.GetQrCodeByUuidAsync(qrCode.Id))
            .ReturnsAsync(qrCode);
        qrCodeRepository
            .Setup(repository => repository.ReactivateQrCodeAsync(qrCode.Id))
            .ReturnsAsync(true);

        var service = CreateFacade(writeService: CreateWriteService(qrCodeRepository.Object));

        await service.ReactivateQrCodeByUuidAsync(qrCode.Id, CreateUser());

        qrCodeRepository.Verify(repository => repository.GetQrCodeByUuidAsync(qrCode.Id), Times.Once);
        qrCodeRepository.Verify(repository => repository.ReactivateQrCodeAsync(qrCode.Id), Times.Once);
        qrCodeRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteQrCodeByUuidAsync_DelegatesToWriteService()
    {
        var qrCode = CreateQrCode();
        var qrCodeRepository = CreateAuthorizedRepository(adminOnly: true);
        qrCodeRepository
            .Setup(repository => repository.GetQrCodeByUuidAsync(qrCode.Id))
            .ReturnsAsync(qrCode);
        qrCodeRepository
            .Setup(repository => repository.DeleteQrCodeAsync(qrCode.Id))
            .ReturnsAsync(true);

        var service = CreateFacade(writeService: CreateWriteService(qrCodeRepository.Object, adminOnly: true));

        await service.DeleteQrCodeByUuidAsync(qrCode.Id, CreateUser());

        qrCodeRepository.Verify(repository => repository.GetQrCodeByUuidAsync(qrCode.Id), Times.Once);
        qrCodeRepository.Verify(repository => repository.DeleteQrCodeAsync(qrCode.Id), Times.Once);
        qrCodeRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExtendQrCodeExpirationByUuidAsync_DelegatesToWriteService()
    {
        var originalExpiration = DateTime.UtcNow.AddMinutes(5);
        var qrCode = CreateQrCode(expiresAt: originalExpiration);
        var qrCodeRepository = CreateAuthorizedRepository();
        qrCodeRepository
            .Setup(repository => repository.GetQrCodeByUuidAsync(qrCode.Id))
            .ReturnsAsync(qrCode);
        qrCodeRepository
            .Setup(repository => repository.UpdateQrCodeAsync(It.IsAny<QrCode>()))
            .ReturnsAsync((QrCode updated) => updated);

        var service = CreateFacade(writeService: CreateWriteService(qrCodeRepository.Object));

        var response = await service.ExtendQrCodeExpirationByUuidAsync(qrCode.Id, 10, CreateUser());

        Assert.NotNull(response);
        qrCodeRepository.Verify(repository => repository.GetQrCodeByUuidAsync(qrCode.Id), Times.Once);
        qrCodeRepository.Verify(repository => repository.UpdateQrCodeAsync(It.Is<QrCode>(entity => entity.Id == qrCode.Id && entity.ExpiresAt == originalExpiration.AddMinutes(10))), Times.Once);
        qrCodeRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateQrCodeAsync_CanonicalSessionId_ResolvesSessionIdBeforePersisting()
    {
        var sessionUuid = Guid.NewGuid();
        var capturedSessionId = Guid.NewGuid();
        var qrCodeRepository = CreateAuthorizedRepository();
        var sessionRepository = new Mock<ISessionRepository>();
        sessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(new Session { Id = sessionUuid, Status = SessionStatusConstants.Active });
        sessionRepository
            .Setup(repository => repository.GetSessionByIdAsync(sessionUuid))
            .ReturnsAsync(new Session { Id = sessionUuid, Status = SessionStatusConstants.Active });
        qrCodeRepository
            .Setup(repository => repository.CreateQrCodeAsync(It.IsAny<QrCode>()))
            .Callback<QrCode>(entity => capturedSessionId = entity.SessionId)
            .ReturnsAsync((QrCode entity) => entity);

        var service = CreateFacade(generationService: CreateGenerationService(qrCodeRepository.Object, sessionRepository.Object));

        await service.CreateQrCodeAsync(
            new CreateQrCode
            {
                SessionId = sessionUuid,
                QrHash = "uuid-backed-hash",
                ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            },
            CreateUser());

        Assert.Equal(sessionUuid, capturedSessionId);
        sessionRepository.Verify(repository => repository.GetSessionByIdAsync(sessionUuid), Times.Once);
    }

    [Fact]
    public async Task GenerateQrCodeAsync_CanonicalSessionId_ResolvesSessionIdBeforePersisting()
    {
        var sessionUuid = Guid.NewGuid();
        var capturedSessionId = Guid.Empty;
        var qrCodeRepository = CreateAuthorizedRepository();
        var sessionRepository = new Mock<ISessionRepository>();
        sessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(new Session { Id = sessionUuid, Status = SessionStatusConstants.Active });
        sessionRepository
            .Setup(repository => repository.GetSessionByIdAsync(sessionUuid))
            .ReturnsAsync(new Session { Id = sessionUuid, Status = SessionStatusConstants.Active });
        qrCodeRepository
            .Setup(repository => repository.QrHashExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        qrCodeRepository
            .Setup(repository => repository.CreateQrCodeAsync(It.IsAny<QrCode>()))
            .Callback<QrCode>(entity => capturedSessionId = entity.SessionId)
            .ReturnsAsync((QrCode entity) => entity);

        var service = CreateFacade(generationService: CreateGenerationService(qrCodeRepository.Object, sessionRepository.Object));

        var result = await service.GenerateQrCodeAsync(
            new QrCodeRequest
            {
                SessionId = sessionUuid,
                ExpirationMinutes = 15,
                UniqueHash = "client-seed"
            },
            CreateUser());

        Assert.True(result.Success);
        Assert.Equal(sessionUuid, capturedSessionId);
        sessionRepository.Verify(repository => repository.GetSessionByIdAsync(sessionUuid), Times.Once);
    }

    private static QrCodeService CreateFacade(
        QrCodeQueryService? queryService = null,
        QrCodeWriteService? writeService = null,
        QrCodeGenerationService? generationService = null)
    {
        return new QrCodeService(
            queryService ?? new QrCodeQueryService(Mock.Of<IQrCodeRepository>(), CreateDbContext(), NullLogger<QrCodeQueryService>.Instance),
            writeService ?? CreateWriteService(Mock.Of<IQrCodeRepository>()),
            generationService ?? CreateGenerationService(Mock.Of<IQrCodeRepository>(), Mock.Of<ISessionRepository>()),
            new QrCodeScanService(
                CreateDbContext(),
                Mock.Of<IQrCodeRepository>(),
                Mock.Of<IStudentRepository>(),
                Mock.Of<IAttendanceService>(),
                Mock.Of<IAttendanceRepository>(),
                Mock.Of<INotificationService>(),
                CreateAuthorizationService(Mock.Of<ISessionRepository>()),
                NullLogger<QrCodeScanService>.Instance,
                new ConfiguredTimeZoneProvider(new TimeZoneSettings(), TimeProvider.System, _ => TimeZoneInfo.Local)));
    }

    private static QrCodeWriteService CreateWriteService(IQrCodeRepository qrCodeRepository, bool adminOnly = false)
        => new(
            qrCodeRepository,
            CreateAuthorizationService(Mock.Of<ISessionRepository>(), adminOnly),
            NullLogger<QrCodeWriteService>.Instance);

    private static QrCodeGenerationService CreateGenerationService(IQrCodeRepository qrCodeRepository, ISessionRepository sessionRepository)
        => new(
            qrCodeRepository,
            Mock.Of<INotificationService>(),
            CreateAuthorizationService(sessionRepository),
            sessionRepository,
            NullLogger<QrCodeGenerationService>.Instance);

    private static QrCodeAuthorizationService CreateAuthorizationService(ISessionRepository sessionRepository, bool adminOnly = false)
    {
        var userContextService = new Mock<IUserContextService>();
        userContextService
            .Setup(service => service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync("user-1");
        userContextService
            .Setup(service => service.IsAuthorizedAsync(
                It.IsAny<ClaimsPrincipal>(),
                "user-1",
                It.IsAny<string[]>()))
            .ReturnsAsync(true);

        return new QrCodeAuthorizationService(
            sessionRepository,
            Mock.Of<IStudentEnrollmentService>(),
            userContextService.Object,
            NullLogger<QrCodeAuthorizationService>.Instance);
    }

    private static Mock<IQrCodeRepository> CreateAuthorizedRepository(bool adminOnly = false)
    {
        var repository = new Mock<IQrCodeRepository>();
        repository.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(1);
        return repository;
    }

    private static ClaimsPrincipal CreateUser()
        => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "Test"));

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static QrCode CreateQrCode(Guid? uuid = null, DateTime? expiresAt = null, bool isActive = true)
    {
        return new QrCode
        {
            Id = uuid ?? Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Session = new Session
            {
                Id = Guid.NewGuid(),
                Schedule = new Schedules
                {
                    Subject = new Subject { Name = "Software Engineering" },
                    Section = new Section { Name = "BSCS-3A" },
                    Instructor = new Instructor { Firstname = "Ada", Lastname = "Lovelace", UserId = "inst-1" }
                },
                ActualRoom = new Classroom { Name = "Room 201" }
            },
            QrHash = "qr-hash",
            GeneratedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(5),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}
