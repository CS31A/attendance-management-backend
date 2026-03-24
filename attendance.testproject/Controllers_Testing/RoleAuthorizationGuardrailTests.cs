using attendance_monitoring.Constants;
using attendance_monitoring.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance.testproject.Controllers_Testing;

public class RoleAuthorizationGuardrailTests
{
    [Fact]
    public void AuthorizeRoleAttributes_UseOnlyCanonicalRoles()
    {
        var allowedRoles = new HashSet<string>(StringComparer.Ordinal)
        {
            RoleConstants.Admin,
            RoleConstants.Instructor,
            RoleConstants.Student
        };

        var offenders = typeof(SessionController).Assembly
            .GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type => GetAuthorizeAttributes(type).Select(attribute => (type, attribute)))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.attribute.Roles))
            .SelectMany(entry => entry.attribute.Roles!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(role => (entry.type, role)))
            .Where(entry => !allowedRoles.Contains(entry.role))
            .Select(entry => $"{entry.type.FullName} => {entry.role}")
            .ToList();

        Assert.True(offenders.Count == 0,
            $"Non-canonical roles found in [Authorize] attributes: {string.Join(", ", offenders)}");
    }

    private static IEnumerable<AuthorizeAttribute> GetAuthorizeAttributes(Type controllerType)
    {
        foreach (var attribute in controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>())
        {
            yield return attribute;
        }

        foreach (var method in controllerType.GetMethods())
        {
            foreach (var attribute in method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>())
            {
                yield return attribute;
            }
        }
    }
}
