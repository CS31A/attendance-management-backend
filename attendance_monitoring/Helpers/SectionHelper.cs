using attendance_monitoring.Classes;

namespace attendance_monitoring.Helpers;

public static class SectionHelper
{
    public static Course GetRequiredCourse(Section section)
    {
        return section.Course
            ?? throw new InvalidOperationException($"Section {section.Id} is missing required course data.");
    }
}
