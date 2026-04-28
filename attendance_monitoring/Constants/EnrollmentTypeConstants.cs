using System;
using System.Collections.Generic;
using System.Linq;

namespace attendance_monitoring.Constants;

public static class EnrollmentTypeConstants
{
    public const string Regular = "Regular";
    public const string Irregular = "Irregular";
    public const string Retake = "Retake";

    public static IReadOnlyList<string> All { get; } =
    [
        Regular,
        Irregular,
        Retake
    ];

    public static bool IsValid(string? enrollmentType)
    {
        return !string.IsNullOrWhiteSpace(enrollmentType)
               && All.Any(candidate => string.Equals(candidate, enrollmentType, StringComparison.OrdinalIgnoreCase));
    }

    public static string Normalize(string? enrollmentType)
    {
        if (string.IsNullOrWhiteSpace(enrollmentType))
        {
            return string.Empty;
        }

        return All.FirstOrDefault(candidate => string.Equals(candidate, enrollmentType, StringComparison.OrdinalIgnoreCase))
               ?? enrollmentType;
    }
}
