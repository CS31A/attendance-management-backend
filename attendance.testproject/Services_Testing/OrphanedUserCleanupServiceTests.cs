using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Unit tests for OrphanedUserCleanupService.
/// These tests verify the detection and cleanup of orphaned Identity users
/// (users without corresponding Student, Instructor, or Admin profiles).
/// </summary>
public class OrphanedUserCleanupServiceTests : IDisposable
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<OrphanedUserCleanupService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;

    public OrphanedUserCleanupServiceTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<OrphanedUserCleanupService>>();

        // Setup in-memory configuration
        var configValues = new Dictionary<string, string?>
        {
            {"OrphanedUserCleanup:IntervalHours", "24"},
            {"OrphanedUserCleanup:Enabled", "true"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        // Setup in-memory database with transaction warnings suppressed
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup UserManager mock
        var mockUserStore = new Mock<IUserStore<IdentityUser>>();
        _mockUserManager = new Mock<UserManager<IdentityUser>>(
            mockUserStore.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        // Setup scope factory chain
        _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);

        // Setup service provider to return context and user manager
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(ApplicationDbContext)))
            .Returns(_context);
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(UserManager<IdentityUser>)))
            .Returns(_mockUserManager.Object);
    }

    #region DetectOrphanedUsersAsync Tests

    [Fact]
    public async Task DetectOrphanedUsersAsync_ReturnsEmptyList_WhenNoOrphanedUsers()
    {
        // Arrange
        var user1 = new IdentityUser { Id = "user-1", Email = "student@test.com", UserName = "student@test.com" };
        var user2 = new IdentityUser { Id = "user-2", Email = "instructor@test.com", UserName = "instructor@test.com" };

        _context.Users.AddRange(user1, user2);
        _context.Students.Add(new Student { Id = Guid.NewGuid(), UserId = "user-1", Firstname = "Test", Lastname = "Student", SectionId = Guid.NewGuid() });
        _context.Instructors.Add(new Instructor { Id = Guid.NewGuid(), UserId = "user-2", Firstname = "Test", Lastname = "Instructor" });
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var orphanedUsers = await service.DetectOrphanedUsersAsync();

        // Assert
        Assert.Empty(orphanedUsers);
    }

    [Fact]
    public async Task DetectOrphanedUsersAsync_ReturnsOrphanedUsers_WhenUsersHaveNoProfile()
    {
        // Arrange
        var orphanedUser = new IdentityUser { Id = "orphaned-user", Email = "orphaned@test.com", UserName = "orphaned@test.com" };
        var validUser = new IdentityUser { Id = "valid-user", Email = "valid@test.com", UserName = "valid@test.com" };

        _context.Users.AddRange(orphanedUser, validUser);
        _context.Students.Add(new Student { Id = Guid.NewGuid(), UserId = "valid-user", Firstname = "Valid", Lastname = "User", SectionId = Guid.NewGuid() });
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var orphanedUsers = await service.DetectOrphanedUsersAsync();

        // Assert
        var orphanedList = orphanedUsers.ToList();
        Assert.Single(orphanedList);
        Assert.Equal("orphaned-user", orphanedList[0].UserId);
        Assert.Equal("orphaned@test.com", orphanedList[0].Email);
    }

    [Fact]
    public async Task DetectOrphanedUsersAsync_ReturnsMultipleOrphanedUsers()
    {
        // Arrange
        var orphanedUser1 = new IdentityUser { Id = "orphaned-1", Email = "orphaned1@test.com", UserName = "orphaned1@test.com" };
        var orphanedUser2 = new IdentityUser { Id = "orphaned-2", Email = "orphaned2@test.com", UserName = "orphaned2@test.com" };
        var orphanedUser3 = new IdentityUser { Id = "orphaned-3", Email = "orphaned3@test.com", UserName = "orphaned3@test.com" };

        _context.Users.AddRange(orphanedUser1, orphanedUser2, orphanedUser3);
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var orphanedUsers = await service.DetectOrphanedUsersAsync();

        // Assert
        Assert.Equal(3, orphanedUsers.Count());
    }

    [Fact]
    public async Task DetectOrphanedUsersAsync_DoesNotIncludeUsersWithStudentProfile()
    {
        // Arrange
        var user = new IdentityUser { Id = "student-user", Email = "student@test.com", UserName = "student@test.com" };
        _context.Users.Add(user);
        _context.Students.Add(new Student { Id = Guid.NewGuid(), UserId = "student-user", Firstname = "Test", Lastname = "Student", SectionId = Guid.NewGuid() });
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var orphanedUsers = await service.DetectOrphanedUsersAsync();

        // Assert
        Assert.Empty(orphanedUsers);
    }

    [Fact]
    public async Task DetectOrphanedUsersAsync_DoesNotIncludeUsersWithInstructorProfile()
    {
        // Arrange
        var user = new IdentityUser { Id = "instructor-user", Email = "instructor@test.com", UserName = "instructor@test.com" };
        _context.Users.Add(user);
        _context.Instructors.Add(new Instructor { Id = Guid.NewGuid(), UserId = "instructor-user", Firstname = "Test", Lastname = "Instructor" });
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var orphanedUsers = await service.DetectOrphanedUsersAsync();

        // Assert
        Assert.Empty(orphanedUsers);
    }

    [Fact]
    public async Task DetectOrphanedUsersAsync_DoesNotIncludeUsersWithAdminProfile()
    {
        // Arrange
        var user = new IdentityUser { Id = "admin-user", Email = "admin@test.com", UserName = "admin@test.com" };
        _context.Users.Add(user);
        _context.Admins.Add(new Admin { Id = Guid.NewGuid(), UserId = "admin-user", Firstname = "Test", Lastname = "Admin" });
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var orphanedUsers = await service.DetectOrphanedUsersAsync();

        // Assert
        Assert.Empty(orphanedUsers);
    }

    #endregion

    #region CleanupOrphanedUserAsync Tests

    [Fact]
    public async Task CleanupOrphanedUserAsync_ReturnsFalse_WhenUserHasProfile()
    {
        // Arrange
        var userId = "user-with-profile";
        var user = new IdentityUser { Id = userId, Email = "user@test.com", UserName = "user@test.com" };
        _context.Users.Add(user);
        _context.Students.Add(new Student { Id = Guid.NewGuid(), UserId = userId, Firstname = "Test", Lastname = "Student", SectionId = Guid.NewGuid() });
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var result = await service.CleanupOrphanedUserAsync(userId);

        // Assert
        Assert.False(result);
        _mockUserManager.Verify(um => um.DeleteAsync(It.IsAny<IdentityUser>()), Times.Never);
    }

    [Fact]
    public async Task CleanupOrphanedUserAsync_ReturnsFalse_WhenUserNotFound()
    {
        // Arrange
        var userId = "non-existent-user";
        _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync((IdentityUser?)null);

        var service = CreateService();

        // Act
        var result = await service.CleanupOrphanedUserAsync(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CleanupOrphanedUserAsync_ReturnsTrue_WhenOrphanedUserDeleted()
    {
        // Arrange
        var userId = "orphaned-user";
        var user = new IdentityUser { Id = userId, Email = "orphaned@test.com", UserName = "orphaned@test.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockUserManager.Setup(um => um.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        var service = CreateService();

        // Act
        var result = await service.CleanupOrphanedUserAsync(userId);

        // Assert
        Assert.True(result);
        _mockUserManager.Verify(um => um.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task CleanupOrphanedUserAsync_RemovesRefreshTokens_BeforeDeletingUser()
    {
        // Arrange
        var userId = "orphaned-user";
        var user = new IdentityUser { Id = userId, Email = "orphaned@test.com", UserName = "orphaned@test.com" };
        _context.Users.Add(user);
        _context.RefreshTokens.Add(new RefreshToken { Id = 1, UserId = userId, TokenHash = "hash1", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        _context.RefreshTokens.Add(new RefreshToken { Id = 2, UserId = userId, TokenHash = "hash2", ExpiresAt = DateTime.UtcNow.AddDays(1) });
        await _context.SaveChangesAsync();

        _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockUserManager.Setup(um => um.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        var service = CreateService();

        // Act
        var result = await service.CleanupOrphanedUserAsync(userId);

        // Assert
        Assert.True(result);
        
        // Verify refresh tokens were removed
        var remainingTokens = await _context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
        Assert.Empty(remainingTokens);
    }

    [Fact]
    public async Task CleanupOrphanedUserAsync_ReturnsFalse_WhenDeleteFails()
    {
        // Arrange
        var userId = "orphaned-user";
        var user = new IdentityUser { Id = userId, Email = "orphaned@test.com", UserName = "orphaned@test.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockUserManager.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
        _mockUserManager.Setup(um => um.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed" }));

        var service = CreateService();

        // Act
        var result = await service.CleanupOrphanedUserAsync(userId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CleanupAllOrphanedUsersAsync Tests

    [Fact]
    public async Task CleanupAllOrphanedUsersAsync_ReturnsZero_WhenNoOrphanedUsers()
    {
        // Arrange
        var user = new IdentityUser { Id = "valid-user", Email = "valid@test.com", UserName = "valid@test.com" };
        _context.Users.Add(user);
        _context.Students.Add(new Student { Id = Guid.NewGuid(), UserId = "valid-user", Firstname = "Valid", Lastname = "User", SectionId = Guid.NewGuid() });
        await _context.SaveChangesAsync();

        var service = CreateService();

        // Act
        var cleanedCount = await service.CleanupAllOrphanedUsersAsync();

        // Assert
        Assert.Equal(0, cleanedCount);
    }

    [Fact]
    public async Task CleanupAllOrphanedUsersAsync_CleansAllOrphanedUsers()
    {
        // Arrange
        var orphanedUser1 = new IdentityUser { Id = "orphaned-1", Email = "orphaned1@test.com", UserName = "orphaned1@test.com" };
        var orphanedUser2 = new IdentityUser { Id = "orphaned-2", Email = "orphaned2@test.com", UserName = "orphaned2@test.com" };
        _context.Users.AddRange(orphanedUser1, orphanedUser2);
        await _context.SaveChangesAsync();

        _mockUserManager.Setup(um => um.FindByIdAsync("orphaned-1")).ReturnsAsync(orphanedUser1);
        _mockUserManager.Setup(um => um.FindByIdAsync("orphaned-2")).ReturnsAsync(orphanedUser2);
        _mockUserManager.Setup(um => um.DeleteAsync(It.IsAny<IdentityUser>())).ReturnsAsync(IdentityResult.Success);

        var service = CreateService();

        // Act
        var cleanedCount = await service.CleanupAllOrphanedUsersAsync();

        // Assert
        Assert.Equal(2, cleanedCount);
        _mockUserManager.Verify(um => um.DeleteAsync(It.IsAny<IdentityUser>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CleanupAllOrphanedUsersAsync_ContinuesOnIndividualFailure()
    {
        // Arrange
        var orphanedUser1 = new IdentityUser { Id = "orphaned-1", Email = "orphaned1@test.com", UserName = "orphaned1@test.com" };
        var orphanedUser2 = new IdentityUser { Id = "orphaned-2", Email = "orphaned2@test.com", UserName = "orphaned2@test.com" };
        var orphanedUser3 = new IdentityUser { Id = "orphaned-3", Email = "orphaned3@test.com", UserName = "orphaned3@test.com" };
        _context.Users.AddRange(orphanedUser1, orphanedUser2, orphanedUser3);
        await _context.SaveChangesAsync();

        _mockUserManager.Setup(um => um.FindByIdAsync("orphaned-1")).ReturnsAsync(orphanedUser1);
        _mockUserManager.Setup(um => um.FindByIdAsync("orphaned-2")).ReturnsAsync(orphanedUser2);
        _mockUserManager.Setup(um => um.FindByIdAsync("orphaned-3")).ReturnsAsync(orphanedUser3);

        // First and third delete succeed, second fails
        _mockUserManager.Setup(um => um.DeleteAsync(orphanedUser1)).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(um => um.DeleteAsync(orphanedUser2))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed" }));
        _mockUserManager.Setup(um => um.DeleteAsync(orphanedUser3)).ReturnsAsync(IdentityResult.Success);

        var service = CreateService();

        // Act
        var cleanedCount = await service.CleanupAllOrphanedUsersAsync();

        // Assert
        Assert.Equal(2, cleanedCount); // Only 2 succeeded
        _mockUserManager.Verify(um => um.DeleteAsync(It.IsAny<IdentityUser>()), Times.Exactly(3));
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Constructor_UsesDefaultInterval_WhenNotConfigured()
    {
        // Arrange - Create configuration with no values set
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act - just verifying no exception
        var service = new OrphanedUserCleanupService(
            _mockScopeFactory.Object,
            _mockLogger.Object,
            emptyConfig);

        // Assert - service was created successfully
        Assert.NotNull(service);
    }

    #endregion

    #region Helper Methods

    private OrphanedUserCleanupService CreateService()
    {
        return new OrphanedUserCleanupService(
            _mockScopeFactory.Object,
            _mockLogger.Object,
            _configuration);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
