using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Integration_Testing.Support;

internal static class AccountSeedData
{
    public static async Task<AccountScenarioContext> SeedUserAsync(
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        string role,
        string initialPassword,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        // Ensure roles exist
        var roles = new[] { "Student", "Instructor", "Admin" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var now = DateTime.UtcNow;

        // Create user with hashed password via Identity
        var user = new IdentityUser
        {
            Id = $"{role.ToLower()}-user",
            UserName = $"{role.ToLower()}@test.com",
            Email = $"{role.ToLower()}@test.com",
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, initialPassword);
        await userManager.AddToRoleAsync(user, role);

        // Create role-specific entity
        int? roleSpecificId = null;

        switch (role)
        {
            case "Student":
                var section = new Section
                {
                    Name = "TEST-SEC-A",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var course = new Course
                {
                    Name = "Test Course",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                section.Course = course;

                var student = new Student
                {
                    UserId = user.Id,
                    Firstname = "Test",
                    Lastname = role,
                    Section = section,
                    IsRegular = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                dbContext.AddRange(course, section, student);
                await dbContext.SaveChangesAsync(cancellationToken);
                roleSpecificId = student.Id;
                break;

            case "Instructor":
                var instructor = new Instructor
                {
                    UserId = user.Id,
                    Firstname = "Test",
                    Lastname = role,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                dbContext.Instructors.Add(instructor);
                await dbContext.SaveChangesAsync(cancellationToken);
                roleSpecificId = instructor.Id;
                break;

            case "Admin":
                // Admin has no additional entity
                break;
        }

        return new AccountScenarioContext
        {
            UserId = user.Id,
            Email = user.Email,
            Role = role,
            RoleSpecificId = roleSpecificId
        };
    }
}
