using System.Security.Claims;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Extensions.ServiceCollectionExtensions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Services;
using attendance_monitoring.Services.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;

namespace attendance.testproject.Services_Testing;

public class RoleStandardizationTests
{
    [Fact]
    public void NormalizeRole_ReturnsInstructor_ForLegacyTeacher()
    {
        Assert.Equal(RoleConstants.Instructor, RoleConstants.NormalizeRole(RoleConstants.LegacyTeacher));
    }

    [Theory]
    [InlineData(RoleConstants.Instructor)]
    [InlineData(RoleConstants.LegacyTeacher)]
    [InlineData("instructor")]
    public void IsInstructorRole_ReturnsTrue_ForCanonicalAndLegacyInstructorValues(string role)
    {
        Assert.True(RoleConstants.IsInstructorRole(role));
    }

    [Fact]
    public async Task OnTokenValidated_NormalizesLegacyTeacherClaimToInstructor()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:Token"] = "12345678901234567890123456789012",
                ["AppSettings:Issuer"] = "TestIssuer",
                ["AppSettings:Audience"] = "TestAudience"
            })
            .Build();

        var tokenValidationService = new Mock<ITokenValidationService>();
        tokenValidationService
            .Setup(service => service.IsTokenBlacklistedAsync("jti-1"))
            .ReturnsAsync(false);

        var services = new ServiceCollection();
        services.AddSingleton(tokenValidationService.Object);
        services.AddAuthenticationServices(configuration);

        var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };

        var scheme = new AuthenticationScheme(
            JwtBearerDefaults.AuthenticationScheme,
            JwtBearerDefaults.AuthenticationScheme,
            typeof(JwtBearerHandler));

        var context = new TokenValidatedContext(httpContext, scheme, options)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim(ClaimTypes.Role, RoleConstants.LegacyTeacher),
                new Claim("jti", "jti-1")
            ],
            JwtBearerDefaults.AuthenticationScheme))
        };

        await options.Events.OnTokenValidated(context);

        Assert.Contains(context.Principal!.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == RoleConstants.Instructor);
        Assert.DoesNotContain(context.Principal!.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == RoleConstants.LegacyTeacher);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsValidationException_ForLegacyTeacherRoleInput()
    {
        var accountRepository = new Mock<IAccountRepository>();
        accountRepository.Setup(repository => repository.FindUserByUsernameAsync("legacy-teacher")).ReturnsAsync((IdentityUser?)null);
        accountRepository.Setup(repository => repository.FindUserByEmailAsync("legacy-teacher@test.com")).ReturnsAsync((IdentityUser?)null);

        var sectionRepository = new Mock<ISectionRepository>();
        var userFactory = new Mock<IUserFactory>();

        var service = new RegistrationService(
            accountRepository.Object,
            sectionRepository.Object,
            userFactory.Object,
            NullLogger<RegistrationService>.Instance);

        var registerDto = new RegisterDto
        {
            Username = "legacy-teacher",
            Email = "legacy-teacher@test.com",
            Password = "Test@123",
            RepeatedPassword = "Test@123",
            Role = RoleConstants.LegacyTeacher
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.RegisterAsync(registerDto));

        Assert.Equal("Invalid role specified. Valid roles are: Student, Instructor, Admin", exception.Message);
        userFactory.Verify(factory => factory.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()), Times.Never);
    }
}
