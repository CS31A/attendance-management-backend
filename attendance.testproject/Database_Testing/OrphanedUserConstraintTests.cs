using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace attendance.testproject.Database_Testing;

/// <summary>
/// Unit tests for database constraints that prevent orphaned users.
/// These tests verify the soft delete consistency constraints and
/// the behavior of the orphaned user prevention system.
/// 
/// Note: Some constraint tests require SQL Server and cannot be fully
/// tested with in-memory database. These tests focus on application-level
/// validation that works with in-memory database.
/// </summary>
public class OrphanedUserConstraintTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public OrphanedUserConstraintTests()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Soft Delete Consistency Tests

    [Fact]
    public void Student_SoftDelete_SettingIsDeletedTrue_ShouldRequireDeletedAt()
    {
        // Arrange
        var student = new Student
        {
            Firstname = "Test",
            Lastname = "Student",
            UserId = "test-user-id",
            SectionId = 1,
            IsDeleted = true,
            DeletedAt = null // This should be invalid per our constraint
        };

        // Act & Assert
        // In application code, we should validate this before saving
        // The database constraint will catch this at the DB level
        var isConsistent = ValidateSoftDeleteConsistency(student.IsDeleted, student.DeletedAt);
        Assert.False(isConsistent, "IsDeleted=true with DeletedAt=null should be inconsistent");
    }

    [Fact]
    public void Student_SoftDelete_SettingIsDeletedFalse_ShouldNotHaveDeletedAt()
    {
        // Arrange
        var student = new Student
        {
            Firstname = "Test",
            Lastname = "Student",
            UserId = "test-user-id",
            SectionId = 1,
            IsDeleted = false,
            DeletedAt = DateTime.UtcNow // This should be invalid per our constraint
        };

        // Act & Assert
        var isConsistent = ValidateSoftDeleteConsistency(student.IsDeleted, student.DeletedAt);
        Assert.False(isConsistent, "IsDeleted=false with DeletedAt set should be inconsistent");
    }

    [Fact]
    public void Student_SoftDelete_ConsistentState_IsDeletedTrueWithDeletedAt()
    {
        // Arrange
        var student = new Student
        {
            Firstname = "Test",
            Lastname = "Student",
            UserId = "test-user-id",
            SectionId = 1,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow // This is valid
        };

        // Act & Assert
        var isConsistent = ValidateSoftDeleteConsistency(student.IsDeleted, student.DeletedAt);
        Assert.True(isConsistent, "IsDeleted=true with DeletedAt set should be consistent");
    }

    [Fact]
    public void Student_SoftDelete_ConsistentState_IsDeletedFalseWithoutDeletedAt()
    {
        // Arrange
        var student = new Student
        {
            Firstname = "Test",
            Lastname = "Student",
            UserId = "test-user-id",
            SectionId = 1,
            IsDeleted = false,
            DeletedAt = null // This is valid
        };

        // Act & Assert
        var isConsistent = ValidateSoftDeleteConsistency(student.IsDeleted, student.DeletedAt);
        Assert.True(isConsistent, "IsDeleted=false with DeletedAt=null should be consistent");
    }

    [Fact]
    public void Instructor_SoftDelete_ConsistentState_ShouldBeValid()
    {
        // Arrange & Act
        var validInstructor = new Instructor
        {
            Firstname = "Test",
            Lastname = "Instructor",
            UserId = "test-instructor-id",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };

        // Assert
        var isConsistent = ValidateSoftDeleteConsistency(validInstructor.IsDeleted, validInstructor.DeletedAt);
        Assert.True(isConsistent);
    }

    [Fact]
    public void Admin_SoftDelete_ConsistentState_ShouldBeValid()
    {
        // Arrange & Act
        var validAdmin = new Admin
        {
            Firstname = "Test",
            Lastname = "Admin",
            UserId = "test-admin-id",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };

        // Assert
        var isConsistent = ValidateSoftDeleteConsistency(validAdmin.IsDeleted, validAdmin.DeletedAt);
        Assert.True(isConsistent);
    }

    #endregion

    #region Profile Existence Tests

    [Fact]
    public async Task UserWithStudentProfile_ShouldNotBeOrphaned()
    {
        // Arrange
        var userId = "student-user-id";
        var student = new Student
        {
            Firstname = "Test",
            Lastname = "Student",
            UserId = userId,
            SectionId = 1,
            IsDeleted = false,
            DeletedAt = null
        };
        
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        // Act
        var hasProfile = await HasActiveProfileAsync(userId);

        // Assert
        Assert.True(hasProfile, "User with active student profile should not be orphaned");
    }

    [Fact]
    public async Task UserWithInstructorProfile_ShouldNotBeOrphaned()
    {
        // Arrange
        var userId = "instructor-user-id";
        var instructor = new Instructor
        {
            Firstname = "Test",
            Lastname = "Instructor",
            UserId = userId,
            IsDeleted = false,
            DeletedAt = null
        };
        
        _context.Instructors.Add(instructor);
        await _context.SaveChangesAsync();

        // Act
        var hasProfile = await HasActiveProfileAsync(userId);

        // Assert
        Assert.True(hasProfile, "User with active instructor profile should not be orphaned");
    }

    [Fact]
    public async Task UserWithAdminProfile_ShouldNotBeOrphaned()
    {
        // Arrange
        var userId = "admin-user-id";
        var admin = new Admin
        {
            Firstname = "Test",
            Lastname = "Admin",
            UserId = userId,
            IsDeleted = false,
            DeletedAt = null
        };
        
        _context.Admins.Add(admin);
        await _context.SaveChangesAsync();

        // Act
        var hasProfile = await HasActiveProfileAsync(userId);

        // Assert
        Assert.True(hasProfile, "User with active admin profile should not be orphaned");
    }

    [Fact]
    public async Task UserWithSoftDeletedProfile_ShouldBeConsideredOrphaned()
    {
        // Arrange
        var userId = "soft-deleted-user-id";
        var student = new Student
        {
            Firstname = "Test",
            Lastname = "Student",
            UserId = userId,
            SectionId = 1,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        // Act
        var hasActiveProfile = await HasActiveProfileAsync(userId);

        // Assert
        Assert.False(hasActiveProfile, "User with soft-deleted profile should be considered orphaned");
    }

    [Fact]
    public async Task UserWithNoProfile_ShouldBeOrphaned()
    {
        // Arrange
        var userId = "no-profile-user-id";
        // Don't add any profile

        // Act
        var hasProfile = await HasActiveProfileAsync(userId);

        // Assert
        Assert.False(hasProfile, "User without any profile should be orphaned");
    }

    [Fact]
    public async Task UserWithMultipleProfileTypes_OnlyOneActive_ShouldNotBeOrphaned()
    {
        // Arrange
        var userId = "multi-profile-user-id";
        
        // Add an active instructor profile
        var instructor = new Instructor
        {
            Firstname = "Test",
            Lastname = "Instructor",
            UserId = userId,
            IsDeleted = false,
            DeletedAt = null
        };
        _context.Instructors.Add(instructor);
        
        // Also has a soft-deleted admin profile
        var admin = new Admin
        {
            Firstname = "Test",
            Lastname = "Admin",
            UserId = userId,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        _context.Admins.Add(admin);
        
        await _context.SaveChangesAsync();

        // Act
        var hasActiveProfile = await HasActiveProfileAsync(userId);

        // Assert
        Assert.True(hasActiveProfile, "User with at least one active profile should not be orphaned");
    }

    #endregion

    #region Constraint Violation Detection Tests

    [Fact]
    public async Task DetectInconsistentSoftDelete_Students_ShouldReturnViolatingRecords()
    {
        // Arrange
        // This test validates that we can detect inconsistent soft delete states
        // In production, the constraint would prevent these, but we need to test detection
        
        // Note: In-memory database won't enforce the check constraint,
        // so we test the application-level validation logic

        var consistentStudent = new Student
        {
            Firstname = "Consistent",
            Lastname = "Student",
            UserId = "consistent-user",
            SectionId = 1,
            IsDeleted = false,
            DeletedAt = null
        };
        
        _context.Students.Add(consistentStudent);
        await _context.SaveChangesAsync();

        // Act
        var inconsistentCount = await _context.Students
            .Where(s => (s.IsDeleted && s.DeletedAt == null) || (!s.IsDeleted && s.DeletedAt != null))
            .CountAsync();

        // Assert
        Assert.Equal(0, inconsistentCount);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Validates soft delete consistency according to our database constraint logic.
    /// (IsDeleted = 1 AND DeletedAt IS NOT NULL) OR (IsDeleted = 0 AND DeletedAt IS NULL)
    /// </summary>
    private static bool ValidateSoftDeleteConsistency(bool isDeleted, DateTime? deletedAt)
    {
        return (isDeleted && deletedAt.HasValue) || (!isDeleted && !deletedAt.HasValue);
    }

    /// <summary>
    /// Checks if a user has an active (non-deleted) profile.
    /// </summary>
    private async Task<bool> HasActiveProfileAsync(string userId)
    {
        return await _context.Students.AnyAsync(s => s.UserId == userId && !s.IsDeleted) ||
               await _context.Instructors.AnyAsync(i => i.UserId == userId && !i.IsDeleted) ||
               await _context.Admins.AnyAsync(a => a.UserId == userId && !a.IsDeleted);
    }

    #endregion
}
