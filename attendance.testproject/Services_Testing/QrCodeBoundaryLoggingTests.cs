using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using attendance_monitoring.Services.QrCode;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace attendance.testproject.Services_Testing;

public class QrCodeBoundaryLoggingTests
{
    [Fact]
    public async Task UpdateQrCodeAsync_RepositoryUpdateFails_LogsQrCodeIdContext()
    {
        var qrCodeRepository = new Mock<IQrCodeRepository>();
        var logger = new TestLogger<QrCodeWriteService>();
        var service = new QrCodeWriteService(
            qrCodeRepository.Object,
            CreateAuthorizationService(),
            logger);
        var user = CreateUserPrincipal("user-1");
        var expectedException = new InvalidOperationException("Update failed");

        qrCodeRepository
            .Setup(r => r.GetQrCodeByIdAsync(123))
            .ReturnsAsync(new QrCode { Id = 123, ExpiresAt = DateTime.UtcNow.AddMinutes(10) });
        qrCodeRepository
            .Setup(r => r.UpdateQrCodeAsync(It.IsAny<QrCode>()))
            .ThrowsAsync(expectedException);

        await Assert.ThrowsAsync<EntityServiceException>(() =>
            service.UpdateQrCodeAsync(123, new UpdateQrCode { IsActive = true }, user));

        VerifyErrorLogContains(logger, expectedException, "123");
    }

    [Fact]
    public async Task CreateQrCodeAsync_RepositoryCreateFails_LogsSessionIdContext()
    {
        var qrCodeRepository = new Mock<IQrCodeRepository>();
        var logger = new TestLogger<QrCodeGenerationService>();
        var service = new QrCodeGenerationService(
            qrCodeRepository.Object,
            Mock.Of<INotificationService>(),
            CreateAuthorizationService(sessionId: 42),
            logger);
        var user = CreateUserPrincipal("user-1");
        var expectedException = new InvalidOperationException("Create failed");

        qrCodeRepository
            .Setup(r => r.CreateQrCodeAsync(It.IsAny<QrCode>()))
            .ThrowsAsync(expectedException);

        await Assert.ThrowsAsync<EntityServiceException>(() =>
            service.CreateQrCodeAsync(
                new CreateQrCode
                {
                    SessionId = 42,
                    QrHash = "hash-42",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5)
                },
                user));

        VerifyErrorLogContains(logger, expectedException, "42");
    }

    [Fact]
    public async Task GenerateQrCodeAsync_RepositoryCreateFails_LogsSessionIdContext()
    {
        var qrCodeRepository = new Mock<IQrCodeRepository>();
        var logger = new TestLogger<QrCodeGenerationService>();
        var service = new QrCodeGenerationService(
            qrCodeRepository.Object,
            Mock.Of<INotificationService>(),
            CreateAuthorizationService(sessionId: 42),
            logger);
        var user = CreateUserPrincipal("user-1");
        var expectedException = new InvalidOperationException("Generate failed");

        qrCodeRepository
            .Setup(r => r.QrHashExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        qrCodeRepository
            .Setup(r => r.CreateQrCodeAsync(It.IsAny<QrCode>()))
            .ThrowsAsync(expectedException);

        var result = await service.GenerateQrCodeAsync(
            new QrCodeRequest
            {
                SessionId = 42,
                ExpirationMinutes = 15,
                UniqueHash = "client-seed"
            },
            user);

        Assert.False(result.Success);
        VerifyErrorLogContains(logger, expectedException, "42");
    }

    private static QrCodeAuthorizationService CreateAuthorizationService(int sessionId = 123)
    {
        var sessionRepository = new Mock<ISessionRepository>();
        sessionRepository
            .Setup(r => r.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(new Session { Id = sessionId, Status = "active" });

        var enrollmentService = new Mock<IStudentEnrollmentService>();

        var userStore = new Mock<IUserStore<IdentityUser>>();
        var userManager = new Mock<UserManager<IdentityUser>>(
            userStore.Object,
            Options.Create(new IdentityOptions()),
            Mock.Of<IPasswordHasher<IdentityUser>>(),
            Array.Empty<IUserValidator<IdentityUser>>(),
            Array.Empty<IPasswordValidator<IdentityUser>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<IdentityUser>>>());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        var userContextService = new UserContextService(userManager.Object, context);

        return new QrCodeAuthorizationService(
            sessionRepository.Object,
            enrollmentService.Object,
            userContextService,
            NullLogger<QrCodeAuthorizationService>.Instance);
    }

    private static ClaimsPrincipal CreateUserPrincipal(string userId)
        => new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            "TestAuthentication"));

    private static void VerifyErrorLogContains<T>(TestLogger<T> logger, Exception expectedException, string expectedText)
    {
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Error
                     && entry.Exception == expectedException
                     && entry.Message.Contains(expectedText, StringComparison.Ordinal));
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose() { }
        }
    }
}
