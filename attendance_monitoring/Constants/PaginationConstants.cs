namespace attendance_monitoring.Constants;

/// <summary>
/// Constants for pagination parameters across the application.
/// </summary>
public static class PaginationConstants
{
    /// <summary>
    /// Default page number when not specified (1-indexed).
    /// </summary>
    public const int DefaultPageNumber = 1;

    /// <summary>
    /// Default page size when not specified.
    /// </summary>
    public const int DefaultPageSize = 50;

    /// <summary>
    /// Maximum allowed page size to prevent excessive data retrieval.
    /// </summary>
    public const int MaxPageSize = 1000;

    /// <summary>
    /// Minimum allowed page number.
    /// </summary>
    public const int MinPageNumber = 1;

    /// <summary>
    /// Minimum allowed page size.
    /// </summary>
    public const int MinPageSize = 1;
}
