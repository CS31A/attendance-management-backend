using System;

namespace attendance_monitoring.Constants;

/// <summary>
/// Constants for user roles in the system.
/// </summary>
public static class RoleConstants
{
    public const string Admin = "Admin";
    public const string Instructor = "Instructor";
    public const string Student = "Student";
    public const string LegacyTeacher = "Teacher";

    public static string NormalizeRole(string? role)
    {
        if (string.Equals(role, LegacyTeacher, StringComparison.OrdinalIgnoreCase))
        {
            return Instructor;
        }

        return role ?? string.Empty;
    }

    public static bool IsInstructorRole(string? role)
    {
        return string.Equals(NormalizeRole(role), Instructor, StringComparison.OrdinalIgnoreCase);
    }
}
