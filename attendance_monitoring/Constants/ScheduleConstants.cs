namespace attendance_monitoring.Constants;

/// <summary>
/// Constants for schedule validation and configuration
/// </summary>
public static class ScheduleConstants
{
    /// <summary>
    /// Valid days of the week for scheduling
    /// </summary>
    public static readonly string[] ValidDaysOfWeek =
    [
        "Monday",
        "Tuesday",
        "Wednesday",
        "Thursday",
        "Friday",
        "Saturday",
        "Sunday"
    ];

    /// <summary>
    /// Checks if the provided day is a valid day of the week
    /// </summary>
    /// <param name="day">The day to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidDayOfWeek(string? day)
    {
        if (string.IsNullOrEmpty(day))
            return false;

        return ValidDaysOfWeek.Contains(day, StringComparer.OrdinalIgnoreCase);
    }
}
