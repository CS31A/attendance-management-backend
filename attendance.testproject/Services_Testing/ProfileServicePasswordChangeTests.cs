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
using Moq;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Focused unit tests for password change functionality in ProfileService.UpdateUserProfileAsync.
/// Tests cover all password validation branches and edge cases.
/// </summary>
public class ProfileServicePasswordChangeTests
{
    [Fact]
    public async Task GetUserProfileAsync_ThrowsInvalidOperationException_WhenStudentProfileDataIsMissing()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        var user = new IdentityUser { Id = "student-1", UserName = "student", Email = "student@test.com" };
        accountRepository.Setup(r => r.FindUserByIdAsync("student-1")).ReturnsAsync(user);
        accountRepository.Setup(r => r.GetUserRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });

        context.Students.Add(new Student
        {
            Firstname = "Missing",
            Lastname = "Section",
            UserId = "student-1",
            SectionId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetUserProfileAsync("student-1"));

        Assert.Contains("missing required student profile data", exception.Message);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ThrowsValidationException_WhenNewPasswordProvidedWithoutCurrentPassword()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        var user = new IdentityUser { Id = "user-1", UserName = "testuser", Email = "test@test.com" };
        accountRepository.Setup(r => r.FindUserByIdAsync("user-1")).ReturnsAsync(user);
        accountRepository.Setup(r => r.GetUserRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });

        var service = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        var updateDto = new UpdateProfile
        {
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!",
            // CurrentPassword is intentionally missing
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateUserProfileAsync("user-1", updateDto));

        Assert.Equal("Current password is required to change password", exception.Message);
        accountRepository.Verify(r => r.CheckPasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
        accountRepository.Verify(r => r.ChangePasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ThrowsValidationException_WhenNewPasswordDoesNotMatchConfirmation()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        var user = new IdentityUser { Id = "user-1", UserName = "testuser", Email = "test@test.com" };
        accountRepository.Setup(r => r.FindUserByIdAsync("user-1")).ReturnsAsync(user);
        accountRepository.Setup(r => r.GetUserRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });

        var service = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        var updateDto = new UpdateProfile
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "DifferentPassword123!",
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateUserProfileAsync("user-1", updateDto));

        Assert.Equal("New password and confirmation password do not match", exception.Message);
        accountRepository.Verify(r => r.CheckPasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
        accountRepository.Verify(r => r.ChangePasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ThrowsValidationException_WhenCurrentPasswordIsIncorrect()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        var user = new IdentityUser { Id = "user-1", UserName = "testuser", Email = "test@test.com" };
        accountRepository.Setup(r => r.FindUserByIdAsync("user-1")).ReturnsAsync(user);
        accountRepository.Setup(r => r.GetUserRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });

        // Mock CheckPasswordAsync to fail (incorrect current password)
        accountRepository.Setup(r => r.CheckPasswordAsync(user, "WrongPassword123!"))
            .ReturnsAsync(SignInResult.Failed);

        var service = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        var updateDto = new UpdateProfile
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!",
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateUserProfileAsync("user-1", updateDto));

        Assert.Equal("Current password is incorrect", exception.Message);
        accountRepository.Verify(r => r.CheckPasswordAsync(user, "WrongPassword123!"), Times.Once);
        accountRepository.Verify(r => r.ChangePasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ThrowsValidationException_WhenIdentityPasswordChangeFails()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        var user = new IdentityUser { Id = "user-1", UserName = "testuser", Email = "test@test.com" };
        accountRepository.Setup(r => r.FindUserByIdAsync("user-1")).ReturnsAsync(user);
        accountRepository.Setup(r => r.GetUserRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });

        // Mock CheckPasswordAsync to succeed
        accountRepository.Setup(r => r.CheckPasswordAsync(user, "CorrectPassword123!"))
            .ReturnsAsync(SignInResult.Success);

        // Mock ChangePasswordAsync to fail with Identity errors
        accountRepository.Setup(r => r.ChangePasswordAsync(user, "CorrectPassword123!", "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Password too weak" },
                new IdentityError { Description = "Password must contain special character" }));

        var service = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        var updateDto = new UpdateProfile
        {
            CurrentPassword = "CorrectPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!",
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateUserProfileAsync("user-1", updateDto));

        Assert.StartsWith("Password change failed:", exception.Message);
        Assert.Contains("Password too weak", exception.Message);
        Assert.Contains("Password must contain special character", exception.Message);
        accountRepository.Verify(r => r.CheckPasswordAsync(user, "CorrectPassword123!"), Times.Once);
        accountRepository.Verify(r => r.ChangePasswordAsync(user, "CorrectPassword123!", "NewPassword123!"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_Succeeds_WhenPasswordChangeIsValid()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        var user = new IdentityUser { Id = "user-1", UserName = "testuser", Email = "test@test.com" };
        accountRepository.Setup(r => r.FindUserByIdAsync("user-1")).ReturnsAsync(user);
        accountRepository.Setup(r => r.GetUserRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });

        // Mock password operations to succeed
        accountRepository.Setup(r => r.CheckPasswordAsync(user, "CorrectPassword123!"))
            .ReturnsAsync(SignInResult.Success);
        accountRepository.Setup(r => r.ChangePasswordAsync(user, "CorrectPassword123!", "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Success);
        accountRepository.Setup(r => r.UpdateUserAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        accountRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        await SeedValidStudentProfileAsync(context, user);

        var service = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        var updateDto = new UpdateProfile
        {
            CurrentPassword = "CorrectPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!",
        };

        // Act
        var result = await service.UpdateUserProfileAsync("user-1", updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user-1", result.UserId);

        accountRepository.Verify(r => r.CheckPasswordAsync(user, "CorrectPassword123!"), Times.Once);
        accountRepository.Verify(r => r.ChangePasswordAsync(user, "CorrectPassword123!", "NewPassword123!"), Times.Once);
        accountRepository.Verify(r => r.UpdateUserAsync(user), Times.Once);
        accountRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_Succeeds_WhenEmailAndPasswordChangeTogether()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        var user = new IdentityUser { Id = "user-1", UserName = "testuser", Email = "old@test.com" };
        accountRepository.Setup(r => r.FindUserByIdAsync("user-1")).ReturnsAsync(user);
        accountRepository.Setup(r => r.GetUserRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });
        accountRepository.Setup(r => r.EmailExistsAsync("new@test.com", "user-1")).ReturnsAsync(false);

        // Mock password operations to succeed
        accountRepository.Setup(r => r.CheckPasswordAsync(user, "CorrectPassword123!"))
            .ReturnsAsync(SignInResult.Success);
        accountRepository.Setup(r => r.ChangePasswordAsync(user, "CorrectPassword123!", "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Success);
        accountRepository.Setup(r => r.UpdateUserAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        accountRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        await SeedValidStudentProfileAsync(context, user);

        var service = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        var updateDto = new UpdateProfile
        {
            Email = "new@test.com",
            CurrentPassword = "CorrectPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!",
        };

        // Act
        var result = await service.UpdateUserProfileAsync("user-1", updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user-1", result.UserId);

        // Verify email was updated
        Assert.Equal("new@test.com", user.Email);
        Assert.Equal("NEW@TEST.COM", user.NormalizedEmail);

        // Verify password operations were called
        accountRepository.Verify(r => r.CheckPasswordAsync(user, "CorrectPassword123!"), Times.Once);
        accountRepository.Verify(r => r.ChangePasswordAsync(user, "CorrectPassword123!", "NewPassword123!"), Times.Once);
        accountRepository.Verify(r => r.UpdateUserAsync(user), Times.Once);
        accountRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ThrowsEntityNotFoundException_WhenUserNotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        // Mock user not found
        accountRepository.Setup(r => r.FindUserByIdAsync("nonexistent-user")).ReturnsAsync((IdentityUser?)null);

        var service = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        var updateDto = new UpdateProfile
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!",
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => service.UpdateUserProfileAsync("nonexistent-user", updateDto));

        Assert.Equal("User", exception.EntityName);
        Assert.Equal("nonexistent-user", exception.Key);
        Assert.Equal("User not found", exception.Message);

        // Verify password operations were never called
        accountRepository.Verify(r => r.CheckPasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
        accountRepository.Verify(r => r.ChangePasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_DoesNotAttemptPasswordChange_WhenNewPasswordNotProvided()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var accountRepository = new Mock<IAccountRepository>();
        var sectionRepository = new Mock<ISectionRepository>();
        var instructorRepository = new Mock<IInstructorRepository>();

        var user = new IdentityUser { Id = "user-1", UserName = "testuser", Email = "test@test.com" };
        accountRepository.Setup(r => r.FindUserByIdAsync("user-1")).ReturnsAsync(user);
        accountRepository.Setup(r => r.GetUserRolesAsync(user)).ReturnsAsync(new List<string> { "Student" });
        accountRepository.Setup(r => r.UpdateUserAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        accountRepository.Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        await SeedValidStudentProfileAsync(context, user);

        var service = new ProfileService(
            context,
            accountRepository.Object,
            sectionRepository.Object,
            instructorRepository.Object,
            NullLogger<ProfileService>.Instance);

        // Update profile without password change
        var updateDto = new UpdateProfile
        {
            Email = "newemail@test.com"
        };

        // Act
        var result = await service.UpdateUserProfileAsync("user-1", updateDto);

        // Assert
        Assert.NotNull(result);

        // Verify password operations were NOT called
        accountRepository.Verify(r => r.CheckPasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.Never);
        accountRepository.Verify(r => r.ChangePasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static async Task SeedValidStudentProfileAsync(ApplicationDbContext context, IdentityUser user)
    {
        context.Users.Add(user);

        var course = new Course
        {
            Name = $"Course {Guid.NewGuid()}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var section = new Section
        {
            Name = $"Section {Guid.NewGuid()}",
            Course = course,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Students.Add(new Student
        {
            Firstname = "Test",
            Lastname = "Student",
            UserId = user.Id,
            Section = section,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }
}
