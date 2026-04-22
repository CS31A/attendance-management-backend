using System.Reflection;
using attendance_monitoring.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace attendance.testproject.Controllers_Testing;

public class AttendanceControllerTest
{
    [Fact]
    public void GetSessionAttendance_RequiresPrivilegedPolicy()
    {
        var method = typeof(AttendanceController).GetMethod(nameof(AttendanceController.GetSessionAttendance));
        Assert.NotNull(method);

        var authorizeAttribute = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("PrivilegedPolicy", authorizeAttribute.Policy);
    }
}
