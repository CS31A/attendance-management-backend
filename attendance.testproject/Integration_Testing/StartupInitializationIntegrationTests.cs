using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Extensions.WebApplicationExtensions;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace attendance.testproject.Integration_Testing;

public sealed class StartupInitializationIntegrationTests
{
    [Fact]
    public async Task InitializeApplicationAsync_WithPendingRelationalMigrations_AppliesThemBeforeSeeding()
    {
        await using var connection = new SqliteConnection($"Data Source=file:startup-init-{Guid.NewGuid():N}?mode=memory&cache=shared");
        await connection.OpenAsync();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));

        var seeder = new CountingSeederService();
        builder.Services.AddSingleton<IDataSeederService>(seeder);

        await using var app = builder.Build();

        await app.InitializeApplicationAsync();

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        Assert.Empty(pendingMigrations);
        Assert.Equal(1, seeder.CallCount);
    }

    [Fact]
    public async Task InitializeApplicationAsync_WithInMemoryDatabase_SeedsWithoutMigrationGuard()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"startup-init-{Guid.NewGuid():N}"));

        var seeder = new CountingSeederService();
        builder.Services.AddSingleton<IDataSeederService>(seeder);

        await using var app = builder.Build();

        await app.InitializeApplicationAsync();

        Assert.Equal(1, seeder.CallCount);
    }

    [Fact]
    public async Task InitializeApplicationAsync_WhenRelationalMigrationApplicationFails_SurfacesTheRealFailure()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"startup-init-readonly-{Guid.NewGuid():N}.db");
        await File.WriteAllBytesAsync(databasePath, []);

        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production
            });

            builder.WebHost.UseTestServer();
            builder.Services.AddLogging();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath};Mode=ReadOnly"));

            var seeder = new CountingSeederService();
            builder.Services.AddSingleton<IDataSeederService>(seeder);

            await using var app = builder.Build();

            var exception = await Assert.ThrowsAsync<SqliteException>(() => app.InitializeApplicationAsync());

            Assert.Contains("readonly", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, seeder.CallCount);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task ApplicationDbContext_WithSqlite_GeneratesProfileUuidsWithoutSqlServerDefaults()
    {
        await using var connection = new SqliteConnection($"Data Source=file:uuid-sqlite-{Guid.NewGuid():N}?mode=memory&cache=shared");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var now = DateTime.UtcNow;
        var course = new Course
        {
            Name = $"Course-{Guid.NewGuid():N}",
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var section = new Section
        {
            Name = $"Section-{Guid.NewGuid():N}",
            CourseId = course.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.Sections.Add(section);
        await context.SaveChangesAsync();

        var studentUser = new IdentityUser
        {
            Id = $"student-{Guid.NewGuid():N}",
            UserName = "student@test.local",
            NormalizedUserName = "STUDENT@TEST.LOCAL",
            Email = "student@test.local",
            NormalizedEmail = "STUDENT@TEST.LOCAL"
        };
        var instructorUser = new IdentityUser
        {
            Id = $"instructor-{Guid.NewGuid():N}",
            UserName = "instructor@test.local",
            NormalizedUserName = "INSTRUCTOR@TEST.LOCAL",
            Email = "instructor@test.local",
            NormalizedEmail = "INSTRUCTOR@TEST.LOCAL"
        };
        var adminUser = new IdentityUser
        {
            Id = $"admin-{Guid.NewGuid():N}",
            UserName = "admin@test.local",
            NormalizedUserName = "ADMIN@TEST.LOCAL",
            Email = "admin@test.local",
            NormalizedEmail = "ADMIN@TEST.LOCAL"
        };

        context.Users.AddRange(studentUser, instructorUser, adminUser);
        context.Students.Add(new Student
        {
            UserId = studentUser.Id,
            Firstname = "Sqlite",
            Lastname = "Student",
            IsRegular = true,
            SectionId = section.Id,
            CreatedAt = now,
            UpdatedAt = now
        });
        context.Instructors.Add(new Instructor
        {
            UserId = instructorUser.Id,
            Firstname = "Sqlite",
            Lastname = "Instructor",
            CreatedAt = now,
            UpdatedAt = now
        });
        context.Admins.Add(new Admin
        {
            UserId = adminUser.Id,
            Firstname = "Sqlite",
            Lastname = "Admin",
            CreatedAt = now,
            UpdatedAt = now
        });

        await context.SaveChangesAsync();

        var student = await context.Students.SingleAsync();
        var instructor = await context.Instructors.SingleAsync();
        var admin = await context.Admins.SingleAsync();

        Assert.NotEqual(Guid.Empty, student.Uuid);
        Assert.NotEqual(Guid.Empty, instructor.Uuid);
        Assert.NotEqual(Guid.Empty, admin.Uuid);
        Assert.NotEqual(student.Uuid, instructor.Uuid);
        Assert.NotEqual(student.Uuid, admin.Uuid);
        Assert.NotEqual(instructor.Uuid, admin.Uuid);
    }

    private sealed class CountingSeederService : IDataSeederService
    {
        public int CallCount { get; private set; }

        public Task SeedDataAsync()
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
