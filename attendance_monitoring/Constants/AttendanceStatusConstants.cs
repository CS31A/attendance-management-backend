using System;
using System.Collections.Generic;
using System.Linq;

namespace attendance_monitoring.Constants;

public static class AttendanceStatusConstants
{
    public const string Present = "Present";
    public const string Absent = "Absent";
    public const string Late = "Late";
    public const string Excused = "Excused";

    public static IReadOnlyList<string> All { get; } =
    [
        Present,
        Absent,
        Late,
        Excused
    ];

    public static bool IsValid(string? status)
    {
        return !string.IsNullOrWhiteSpace(status) &&
               All.Any(candidate => string.Equals(candidate, status, StringComparison.OrdinalIgnoreCase));
    }

    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        return All.FirstOrDefault(candidate => string.Equals(candidate, status, StringComparison.OrdinalIgnoreCase))
               ?? status;
    }
}
