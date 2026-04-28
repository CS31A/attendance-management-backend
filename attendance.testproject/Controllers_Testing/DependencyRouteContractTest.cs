using System.Reflection;
using attendance_monitoring.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace attendance.testproject.Controllers_Testing;

public class DependencyRouteContractTest
{
    public static TheoryData<Type, string, string> UuidDependencyRoutes =>
        new()
        {
            { typeof(CourseController), nameof(CourseController.HasSectionsInCourse), "{id:guid}/has-sections" },
            { typeof(SubjectController), nameof(SubjectController.HasSchedulesInSubject), "{id:guid}/has-schedules" },
            { typeof(SubjectController), nameof(SubjectController.HasEnrollmentsInSubject), "{id:guid}/has-enrollments" },
            { typeof(SectionController), nameof(SectionController.GetActiveStudentsBySectionId), "{sectionId:guid}/active-students" },
            { typeof(SectionController), nameof(SectionController.GetAllStudentsBySectionId), "{sectionId:guid}/all-students" },
            { typeof(SectionController), nameof(SectionController.HasStudentsInSection), "{sectionId:guid}/has-students" },
            { typeof(SectionController), nameof(SectionController.HasStudentEnrollmentsInSection), "{sectionId:guid}/has-enrollments" },
            { typeof(SectionController), nameof(SectionController.HasSchedulesInSection), "{sectionId:guid}/has-schedules" },
            { typeof(ClassroomController), nameof(ClassroomController.HasSchedulesInClassroom), "{id:guid}/has-schedules" },
            { typeof(ClassroomController), nameof(ClassroomController.HasSessionsInClassroom), "{id:guid}/has-sessions" },
            { typeof(ScheduleController), nameof(ScheduleController.HasSessionsInSchedule), "{id:guid}/has-sessions" },
        };

    [Theory]
    [MemberData(nameof(UuidDependencyRoutes))]
    public void DependencyCheckRoutes_AcceptPublicUuidIds(Type controllerType, string actionName, string expectedTemplate)
    {
        var action = controllerType.GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);

        var route = action.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(route);
        Assert.Equal(expectedTemplate, route.Template);
    }
}
