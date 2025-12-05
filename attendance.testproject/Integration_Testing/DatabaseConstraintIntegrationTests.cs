using attendance_monitoring.Classes;
using attendance_monitoring.Classes.Factory;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Integration_Testing;

/// <summary>
/// Integration tests for database constraint scenarios.
/// Tests the end-to-end behavior of user creation with profile creation
/// to ensure orphaned user prevention works correctly.
/// </summary>
public class DatabaseConstraintIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly Mock<ILogger<UserFactory>> _mockLogger;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<OrphanedUserCleanupService>> _mockCleanupLogger;
    private readonly IConfiguration _configuration;

    public DatabaseConstraintIntegrationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup mocks
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockLogger = new Mock<ILogger<UserFactory>>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockCleanupLogger = new Mock<ILogger<OrphanedUserCleanupService>>();

        // Setup configuration
        var configValues = new Dictionary<string, string?>
        {
            {"OrphanedUserCleanup:IntervalHours", "24"},
            {"OrphanedUserCleanup:MonitoringIntervalMinutes", "60"},
            {"OrphanedUserCleanup:Enabled", "true"},
            {"OrphanedUserCleanup:CleanupEnabled", "true"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        // Setup scope factory chain
        _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ApplicationDbContext)))
            .Returns(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region UserFactory Constraint Handling Tests

    [Fact]
    public async Task UserFactory_CreateStudent_WithValidData_ShouldSucceed()
    {
        // Arrange
        var testUserId = Guid.NewGuid().ToString();
        var testUser = new IdentityUser { Id = testUserId, Email = "test@student.com", UserName = "test@student.com" };

        _mockAccountRepository
            .Setup(r => r.CreateUserAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockAccountRepository
            .Setup(r => r.AddUserToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockAccountRepository
            .Setup(r => r.CreateStudentProfileAsync(It.IsAny<Student>()))
            .Returns(Task.CompletedTask);

        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        var factory = new UserFactory(_mockAccountRepository.Object, _mockLogger.Object);

        // Act
        var result = await factory.CreateUserAsync(
            "test@student.com",
            "test@student.com",
            "Password123!",
            "Student",
            "Test",
            "Student",
            1);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.UserId);
    }

    [Fact]
    public async Task UserFactory_CreateStudent_WithoutSectionId_ShouldFail()
    {
        // Arrange
        var testUser = new IdentityUser { Id = Guid.NewGuid().ToString(), Email = "test@student.com" };

        _mockAccountRepository
            .Setup(r => r.CreateUserAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockAccountRepository
            .Setup(r => r.AddUserToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockAccountRepository
            .Setup(r => r.DeleteUserAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var factory = new UserFactory(_mockAccountRepository.Object, _mockLogger.Object);

        // Act - Create student without sectionId
        var result = await factory.CreateUserAsync(
            "test@student.com",
            "test@student.com",
            "Password123!",
            "Student",
            "Test",
            "Student",
            null); // Missing sectionId

        // Assert
        Assert.False(result.Success);
        Assert.Contains("SectionId is required", result.Errors.FirstOrDefault() ?? "");
        
        // Verify cleanup was called since user was created before validation
        _mockAccountRepository.Verify(r => r.DeleteUserAsync(It.IsAny<IdentityUser>()), Times.Once);
    }

    [Fact]
    public async Task UserFactory_CreateStudent_WithoutFirstname_ShouldCleanupAndFail()
    {
        // Arrange
        var testUser = new IdentityUser { Id = Guid.NewGuid().ToString(), Email = "test@student.com" };

        _mockAccountRepository
            .Setup(r => r.CreateUserAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockAccountRepository
            .Setup(r => r.AddUserToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockAccountRepository
            .Setup(r => r.DeleteUserAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var factory = new UserFactory(_mockAccountRepository.Object, _mockLogger.Object);

        // Act
        var result = await factory.CreateUserAsync(
            "test@student.com",
            "test@student.com",
            "Password123!",
            "Student",
            null, // Missing firstname
            "Student",
            1);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Firstname is required", result.Errors.FirstOrDefault() ?? "");
        
        // Verify cleanup was called
        _mockAccountRepository.Verify(r => r.DeleteUserAsync(It.IsAny<IdentityUser>()), Times.Once);
    }

    [Fact]
    public async Task UserFactory_CreateInstructor_WithDbUpdateException_ShouldCleanupUser()
    {
        // Arrange
        var testUser = new IdentityUser { Id = Guid.NewGuid().ToString(), Email = "test@instructor.com" };

        _mockAccountRepository
            .Setup(r => r.CreateUserAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockAccountRepository
            .Setup(r => r.AddUserToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockAccountRepository
            .Setup(r => r.CreateInstructorProfileAsync(It.IsAny<Instructor>()))
            .Returns(Task.CompletedTask);

        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("Simulated database error"));

        _mockAccountRepository
            .Setup(r => r.DeleteUserAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var factory = new UserFactory(_mockAccountRepository.Object, _mockLogger.Object);

        // Act
        var result = await factory.CreateUserAsync(
            "test@instructor.com",
            "test@instructor.com",
            "Password123!",
            "Instructor",
            "Test",
            "Instructor");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("database error", result.Errors.FirstOrDefault() ?? "", StringComparison.OrdinalIgnoreCase);
        
        // Verify cleanup was called
        _mockAccountRepository.Verify(r => r.DeleteUserAsync(It.IsAny<IdentityUser>()), Times.Once);
    }

    [Fact]
    public async Task UserFactory_CreateUser_WithConstraintViolation_ShouldReturnSpecificError()
    {
        // Arrange
        var testUser = new IdentityUser { Id = Guid.NewGuid().ToString(), Email = "test@admin.com" };

        _mockAccountRepository
            .Setup(r => r.CreateUserAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockAccountRepository
            .Setup(r => r.AddUserToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockAccountRepository
            .Setup(r => r.CreateAdminProfileAsync(It.IsAny<Admin>()))
            .Returns(Task.CompletedTask);

        // Simulate a constraint violation
        var innerException = new Exception("CK_Admins_SoftDeleteConsistency constraint violation");
        _mockAccountRepository
            .Setup(r => r.SaveChangesAsync())
            .ThrowsAsync(new DbUpdateException("Database update failed", innerException));

        _mockAccountRepository
            .Setup(r => r.DeleteUserAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var factory = new UserFactory(_mockAccountRepository.Object, _mockLogger.Object);

        // Act
        var result = await factory.CreateUserAsync(
            "test@admin.com",
            "test@admin.com",
            "Password123!",
            "Admin",
            "Test",
            "Admin");

        // Assert
        Assert.False(result.Success);
        // The error message should indicate a constraint violation or soft delete inconsistency
        Assert.True(
            result.Errors.Any(e => 
                e.Contains("constraint", StringComparison.OrdinalIgnoreCase) || 
                e.Contains("database", StringComparison.OrdinalIgnoreCase) ||
                e.Contains("soft delete", StringComparison.OrdinalIgnoreCase) ||
                e.Contains("profile creation failed", StringComparison.OrdinalIgnoreCase)),
            $"Expected error message about constraint/database/soft delete but got: {string.Join("; ", result.Errors)}");
    }

    [Fact]
    public async Task UserFactory_WithInvalidRole_ShouldCleanupUser()
    {
        // Arrange
        var testUser = new IdentityUser { Id = Guid.NewGuid().ToString(), Email = "test@invalid.com" };

        _mockAccountRepository
            .Setup(r => r.CreateUserAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockAccountRepository
            .Setup(r => r.AddUserToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockAccountRepository
            .Setup(r => r.DeleteUserAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var factory = new UserFactory(_mockAccountRepository.Object, _mockLogger.Object);

        // Act
        var result = await factory.CreateUserAsync(
            "test@invalid.com",
            "test@invalid.com",
            "Password123!",
            "InvalidRole", // Invalid role
            "Test",
            "User");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid role", result.Errors.FirstOrDefault() ?? "");
        
        // Verify cleanup was called
        _mockAccountRepository.Verify(r => r.DeleteUserAsync(It.IsAny<IdentityUser>()), Times.Once);
    }

    #endregion

    #region OrphanedUserCleanupService Integration Tests

    [Fact]
    public async Task CleanupService_DetectsOrphanedUsers_WhenNoProfileExists()
    {
        // Arrange
        var orphanedUser = new IdentityUser { Id = "orphaned-1", Email = "orphan@test.com", UserName = "orphan@test.com" };
        _context.Users.Add(orphanedUser);
        await _context.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(UserManager<IdentityUser>)))
            .Returns(mockUserManager.Object);

        var service = new OrphanedUserCleanupService(
            _mockScopeFactory.Object,
            _mockCleanupLogger.Object,
            _configuration);

        // Act
        var orphanedUsers = await service.DetectOrphanedUsersAsync();

        // Assert
        Assert.Single(orphanedUsers);
        Assert.Equal("orphaned-1", orphanedUsers.First().UserId);
    }

    [Fact]
    public async Task CleanupService_DoesNotDetectOrphans_WhenProfileExists()
    {
        // Arrange
        var userId = "user-with-profile";
        var user = new IdentityUser { Id = userId, Email = "valid@test.com", UserName = "valid@test.com" };
        var student = new Student
        {
            Firstname = "Test",
            Lastname = "Student",
            UserId = userId,
            SectionId = 1,
            IsDeleted = false
        };

        _context.Users.Add(user);
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(UserManager<IdentityUser>)))
            .Returns(mockUserManager.Object);

        var service = new OrphanedUserCleanupService(
            _mockScopeFactory.Object,
            _mockCleanupLogger.Object,
            _configuration);

        // Act
        var orphanedUsers = await service.DetectOrphanedUsersAsync();

        // Assert
        Assert.Empty(orphanedUsers);
    }

    [Fact]
    public async Task CleanupService_GetDataIntegrityStatus_ReturnsHealthyWhenNoIssues()
    {
        // Arrange
        var userId = "healthy-user";
        var user = new IdentityUser { Id = userId, Email = "healthy@test.com", UserName = "healthy@test.com" };
        var student = new Student
        {
            Firstname = "Test",
            Lastname = "Student",
            UserId = userId,
            SectionId = 1,
            IsDeleted = false,
            DeletedAt = null // Consistent soft delete state
        };

        _context.Users.Add(user);
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(UserManager<IdentityUser>)))
            .Returns(mockUserManager.Object);

        var service = new OrphanedUserCleanupService(
            _mockScopeFactory.Object,
            _mockCleanupLogger.Object,
            _configuration);

        // Act
        var status = await service.GetDataIntegrityStatusAsync();

        // Assert
        Assert.True(status.IsHealthy);
        Assert.Equal(0, status.OrphanedUserCount);
        Assert.Equal(0, status.StudentsWithInconsistentSoftDelete);
    }

    [Fact]
    public async Task CleanupService_MonitorOrphanedUsers_ReturnsMonitoringResult()
    {
        // Arrange
        var orphanedUser = new IdentityUser { Id = "monitor-orphan", Email = "monitor@test.com", UserName = "monitor@test.com" };
        _context.Users.Add(orphanedUser);
        await _context.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(UserManager<IdentityUser>)))
            .Returns(mockUserManager.Object);

        var service = new OrphanedUserCleanupService(
            _mockScopeFactory.Object,
            _mockCleanupLogger.Object,
            _configuration);

        // Act
        var result = await service.MonitorOrphanedUsersAsync();

        // Assert
        Assert.Equal(1, result.OrphanedUserCount);
        Assert.Single(result.OrphanedUsers);
        Assert.True(result.CheckedAt <= DateTime.UtcNow);
    }

    #endregion

    #region Helper Methods

    private Mock<UserManager<IdentityUser>> CreateMockUserManager()
    {
        var mockUserStore = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            mockUserStore.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);
    }

    #endregion
}
