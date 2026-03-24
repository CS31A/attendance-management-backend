using System;
using System.Collections.Generic;
using System.Linq;

namespace attendance_monitoring.Constants;

public static class SessionStatusConstants
{
    public const string NotStarted = "not_started";
    public const string Active = "active";
    public const string Ended = "ended";
    public const string Cancelled = "cancelled";

    public static IReadOnlyList<string> All { get; } =
    [
        NotStarted,
        Active,
        Ended,
        Cancelled
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
