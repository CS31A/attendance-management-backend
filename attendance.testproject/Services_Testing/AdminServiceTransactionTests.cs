using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace attendance.testproject.Services_Testing;

public class AdminServiceTransactionTests
{
    [Fact]
    public async Task AdminDeleteUserAsync_ThrowsWhenRefreshTokenRevocationFails()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ThrowingSaveChangesContext(options);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = "target-1",
            TokenHash = "hash-1",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsRevoked = false
        });
        await context.SaveChangesWithoutFailureAsync();

        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        var adminUser = new IdentityUser { Id = "admin-1", UserName = "admin" };
        var targetUser = new IdentityUser { Id = "target-1", UserName = "target" };

        accountRepository.Setup(repository => repository.FindUserByIdAsync("admin-1"))
            .ReturnsAsync(adminUser);
        accountRepository.Setup(repository => repository.FindUserByIdAsync("target-1"))
            .ReturnsAsync(targetUser);
        accountRepository.Setup(repository => repository.GetUserRolesAsync(adminUser))
            .ReturnsAsync(new List<string> { "Admin" });
        accountRepository.Setup(repository => repository.DeleteUserAsyncSP("target-1"))
            .ReturnsAsync((true, "deleted"));

        var profileService = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        var service = new AdminService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            profileService,
            NullLogger<AdminService>.Instance);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => service.AdminDeleteUserAsync("admin-1", "target-1"));

        Assert.Contains("revoke", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminDeleteUserAsync_LogsContextAndWraps_WhenStoredProcedureThrows()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();
        var logger = new TestLogger<AdminService>();

        var adminUser = new IdentityUser { Id = "admin-1", UserName = "admin" };
        var targetUser = new IdentityUser { Id = "target-1", UserName = "target" };
        var expectedException = new InvalidOperationException("Delete failed");

        accountRepository.Setup(repository => repository.FindUserByIdAsync("admin-1")).ReturnsAsync(adminUser);
        accountRepository.Setup(repository => repository.FindUserByIdAsync("target-1")).ReturnsAsync(targetUser);
        accountRepository.Setup(repository => repository.GetUserRolesAsync(adminUser)).ReturnsAsync(new List<string> { "Admin" });
        accountRepository.Setup(repository => repository.DeleteUserAsyncSP("target-1")).ThrowsAsync(expectedException);

        var profileService = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        var service = new AdminService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            profileService,
            logger);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => service.AdminDeleteUserAsync("admin-1", "target-1"));

        Assert.Contains("delete", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Error
                     && entry.Exception == expectedException
                     && entry.Message.Contains("admin-1", StringComparison.Ordinal)
                     && entry.Message.Contains("target-1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AdminHardDeleteUserAsync_LogsContextAndWraps_WhenStoredProcedureThrows()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();
        var logger = new TestLogger<AdminService>();

        var adminUser = new IdentityUser { Id = "admin-1", UserName = "admin" };
        var targetUser = new IdentityUser { Id = "target-1", UserName = "target" };
        var expectedException = new InvalidOperationException("Hard delete failed");

        accountRepository.Setup(repository => repository.FindUserByIdAsync("admin-1")).ReturnsAsync(adminUser);
        accountRepository.Setup(repository => repository.FindUserByIdAsync("target-1")).ReturnsAsync(targetUser);
        accountRepository.Setup(repository => repository.GetUserRolesAsync(adminUser)).ReturnsAsync(new List<string> { "Admin" });
        accountRepository.Setup(repository => repository.HardDeleteUserAsyncSP("target-1")).ThrowsAsync(expectedException);

        var profileService = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        var service = new AdminService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            profileService,
            logger);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => service.AdminHardDeleteUserAsync("admin-1", "target-1"));

        Assert.Contains("delete", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Error
                     && entry.Exception == expectedException
                     && entry.Message.Contains("admin-1", StringComparison.Ordinal)
                     && entry.Message.Contains("target-1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AdminRestoreUserAsync_LogsContextAndWraps_WhenStoredProcedureThrows()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();
        var logger = new TestLogger<AdminService>();

        var adminUser = new IdentityUser { Id = "admin-1", UserName = "admin" };
        var targetUser = new IdentityUser { Id = "target-1", UserName = "target" };
        var expectedException = new InvalidOperationException("Restore failed");

        accountRepository.Setup(repository => repository.FindUserByIdAsync("admin-1")).ReturnsAsync(adminUser);
        accountRepository.Setup(repository => repository.FindUserByIdAsync("target-1")).ReturnsAsync(targetUser);
        accountRepository.Setup(repository => repository.GetUserRolesAsync(adminUser)).ReturnsAsync(new List<string> { "Admin" });
        accountRepository.Setup(repository => repository.RestoreUserAsyncSP("target-1")).ThrowsAsync(expectedException);

        var profileService = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        var service = new AdminService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            profileService,
            logger);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => service.AdminRestoreUserAsync("admin-1", "target-1"));

        Assert.Contains("restore", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Error
                     && entry.Exception == expectedException
                     && entry.Message.Contains("admin-1", StringComparison.Ordinal)
                     && entry.Message.Contains("target-1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AdminUpdateUserProfileAsync_PropagatesException_WhenRepositorySaveFails()
    {
        // Arrange: Setup InMemory context and repositories
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ThrowingSaveChangesContext(options);

        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        var adminUser = new IdentityUser { Id = "admin-1", UserName = "admin" };
        var targetUser = new IdentityUser { Id = "target-1", UserName = "target", Email = "target@test.com" };

        accountRepository.Setup(r => r.FindUserByIdAsync("admin-1")).ReturnsAsync(adminUser);
        accountRepository.Setup(r => r.FindUserByIdAsync("target-1")).ReturnsAsync(targetUser);
        accountRepository.Setup(r => r.GetUserRolesAsync(adminUser)).ReturnsAsync(new List<string> { "Admin" });
        accountRepository.Setup(r => r.GetUserRolesAsync(targetUser)).ReturnsAsync(new List<string> { "Instructor" });
        accountRepository.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);
        accountRepository.Setup(r => r.UpdateUserAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);
        accountRepository.Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new InvalidOperationException("Repository save failed"));

        var profileService = new ProfileService(
            context, accountRepository.Object, sectionRepository.Object,
            instructorRepository.Object, NullLogger<ProfileService>.Instance);

        var service = new AdminService(
            context, accountRepository.Object, sectionRepository.Object,
            profileService, NullLogger<AdminService>.Instance);

        var updateDto = new AdminUpdateUser { UserId = "target-1", Firstname = "Jane" };

        // Act & Assert: Exception from SaveChangesAsync propagates through transaction wrapper
        await Assert.ThrowsAnyAsync<Exception>(
            () => service.AdminUpdateUserProfileAsync("admin-1", updateDto));
    }

    [Fact]
    public async Task UpdateUserProfileAsync_PropagatesException_WhenRepositorySaveFails()
    {
        // Arrange: Setup InMemory context and repositories
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ThrowingSaveChangesContext(options);

        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        var targetUser = new IdentityUser { Id = "user-1", UserName = "user", Email = "user@test.com" };

        accountRepository.Setup(r => r.FindUserByIdAsync("user-1")).ReturnsAsync(targetUser);
        accountRepository.Setup(r => r.GetUserRolesAsync(targetUser)).ReturnsAsync(new List<string> { "Instructor" });
        accountRepository.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(false);
        accountRepository.Setup(r => r.UpdateUserAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);
        accountRepository.Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new InvalidOperationException("Repository save failed"));

        var service = new ProfileService(
            context, accountRepository.Object, sectionRepository.Object,
            instructorRepository.Object, NullLogger<ProfileService>.Instance);

        var updateDto = new UpdateProfile { Firstname = "Jane" };

        // Act & Assert: Exception from SaveChangesAsync propagates through transaction wrapper
        await Assert.ThrowsAnyAsync<Exception>(
            () => service.UpdateUserProfileAsync("user-1", updateDto));
    }

    private sealed class ThrowingSaveChangesContext : ApplicationDbContext
    {
        public ThrowingSaveChangesContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public bool ThrowOnSave { get; set; } = true;

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowOnSave)
            {
                throw new DbUpdateException("Simulated save failure");
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> SaveChangesWithoutFailureAsync(CancellationToken cancellationToken = default)
        {
            ThrowOnSave = false;
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                ThrowOnSave = true;
            }
        }
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
