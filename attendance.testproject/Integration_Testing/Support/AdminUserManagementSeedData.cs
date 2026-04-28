using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Integration_Testing.Support;

internal static class AdminUserManagementSeedData
{
    public static async Task<AdminUserManagementScenarioContext> SeedScenarioAsync(
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        CancellationToken cancellationToken)
    {
        var roles = new[] { "Admin", "Instructor", "Student" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var createRoleResult = await roleManager.CreateAsync(new IdentityRole(role));
                EnsureSucceeded(createRoleResult, $"create role {role}");
            }
        }

        var now = DateTime.UtcNow;
        var course = new Course
        {
            Name = "Integration Testing Course",
            CreatedAt = now,
            UpdatedAt = now
        };
        var primarySection = new Section
        {
            Name = "IT-SEC-A",
            Course = course,
            CreatedAt = now,
            UpdatedAt = now
        };
        var alternateSection = new Section
        {
            Name = "IT-SEC-B",
            Course = course,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Courses.Add(course);
        dbContext.Sections.AddRange(primarySection, alternateSection);
        await dbContext.SaveChangesAsync(cancellationToken);

        var adminUser = await CreateUserAsync(userManager, "admin-user", "admin.integration@gmail.com", "Admin");
        var activeStudentUser = await CreateUserAsync(userManager, "active-student", "student.active@gmail.com", "Student");
        var archivedStudentUser = await CreateUserAsync(userManager, "archived-student", "student.archived@gmail.com", "Student");
        var activeInstructorUser = await CreateUserAsync(userManager, "active-instructor", "instructor.active@gmail.com", "Instructor");
        var conflictStudentUser = await CreateUserAsync(userManager, "conflict-student", "student.conflict@gmail.com", "Student");
        var orphanedUser = await CreateUserAsync(userManager, "orphaned-user", "orphaned.user@gmail.com", "Student");

        dbContext.Admins.Add(new Admin
        {
            UserId = adminUser.Id,
            Firstname = "Ada",
            Lastname = "Admin",
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.Students.AddRange(
            new Student
            {
                UserId = activeStudentUser.Id,
                Firstname = "Alice",
                Lastname = "Active",
                IsRegular = true,
                SectionId = primarySection.Id,
                Usn = "TEST-ACTIVE-001",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Student
            {
                UserId = archivedStudentUser.Id,
                Firstname = "Archie",
                Lastname = "Archived",
                IsRegular = false,
                SectionId = primarySection.Id,
                Usn = "TEST-ARCHIVE-001",
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-1),
                IsDeleted = true,
                DeletedAt = now.AddDays(-1)
            },
            new Student
            {
                UserId = conflictStudentUser.Id,
                Firstname = "Connie",
                Lastname = "Conflict",
                IsRegular = true,
                SectionId = alternateSection.Id,
                Usn = "TEST-CONFLICT-001",
                CreatedAt = now,
                UpdatedAt = now
            });
        dbContext.Instructors.Add(new Instructor
        {
            UserId = activeInstructorUser.Id,
            Firstname = "Ian",
            Lastname = "Instructor",
            CreatedAt = now,
            UpdatedAt = now
        });
        dbContext.RefreshTokens.AddRange(
            new RefreshToken
            {
                UserId = activeStudentUser.Id,
                TokenHash = "active-student-token-hash",
                CreatedAt = now,
                ExpiresAt = now.AddHours(6),
                IsRevoked = false
            },
            new RefreshToken
            {
                UserId = conflictStudentUser.Id,
                TokenHash = "control-token-hash",
                CreatedAt = now,
                ExpiresAt = now.AddHours(6),
                IsRevoked = false
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        var activeStudentRefreshTokenId = await dbContext.RefreshTokens
            .Where(token => token.UserId == activeStudentUser.Id)
            .Select(token => token.Id)
            .SingleAsync(cancellationToken);
        var controlRefreshTokenId = await dbContext.RefreshTokens
            .Where(token => token.UserId == conflictStudentUser.Id)
            .Select(token => token.Id)
            .SingleAsync(cancellationToken);

        return new AdminUserManagementScenarioContext
        {
            AdminUserId = adminUser.Id,
            AdminEmail = adminUser.Email!,
            ActiveStudentUserId = activeStudentUser.Id,
            ActiveStudentEmail = activeStudentUser.Email!,
            ArchivedStudentUserId = archivedStudentUser.Id,
            ArchivedStudentEmail = archivedStudentUser.Email!,
            ActiveInstructorUserId = activeInstructorUser.Id,
            ActiveInstructorEmail = activeInstructorUser.Email!,
            ConflictStudentUserId = conflictStudentUser.Id,
            ConflictStudentEmail = conflictStudentUser.Email!,
            OrphanedUserId = orphanedUser.Id,
            OrphanedUserEmail = orphanedUser.Email!,
            PrimarySectionId = primarySection.Id,
            PrimarySectionUuid = primarySection.Id,
            AlternateSectionId = alternateSection.Id,
            AlternateSectionUuid = alternateSection.Id,
            ActiveStudentRefreshTokenId = activeStudentRefreshTokenId,
            ControlRefreshTokenId = controlRefreshTokenId
        };
    }

    private static async Task<IdentityUser> CreateUserAsync(
        UserManager<IdentityUser> userManager,
        string id,
        string email,
        string role)
    {
        var user = new IdentityUser
        {
            Id = id,
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createUserResult = await userManager.CreateAsync(user, "Test@1234");
        EnsureSucceeded(createUserResult, $"create user {id}");

        var addToRoleResult = await userManager.AddToRoleAsync(user, role);
        EnsureSucceeded(addToRoleResult, $"assign role {role} to user {id}");

        return user;
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Failed to {operation}: {errors}");
    }
}
