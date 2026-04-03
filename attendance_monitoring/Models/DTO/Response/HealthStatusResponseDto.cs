namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Shared health response contract for liveness, readiness, and data-integrity endpoints.
/// </summary>
public class HealthStatusResponseDto
{
    /// <summary>
    /// Overall health status for the requested endpoint.
    /// </summary>
    public string status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the health response was generated.
    /// </summary>
    public DateTime timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Service name reported by the health endpoint.
    /// </summary>
    public string service { get; set; } = string.Empty;

    /// <summary>
    /// Database health details when the endpoint evaluates persistence readiness.
    /// </summary>
    public HealthComponentStatusDto? database { get; set; }

    /// <summary>
    /// Data-integrity details when the endpoint evaluates orphan and soft-delete drift.
    /// </summary>
    public DataIntegrityStatusResponseDto? dataIntegrity { get; set; }
}

/// <summary>
/// Represents an individual dependency health section.
/// </summary>
public class HealthComponentStatusDto
{
    /// <summary>
    /// Health status for the dependency.
    /// </summary>
    public string status { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the dependency is reachable.
    /// </summary>
    public bool? connected { get; set; }

    /// <summary>
    /// Error details when the dependency check fails.
    /// </summary>
    public string? error { get; set; }
}

/// <summary>
/// Represents actionable data-integrity health details.
/// </summary>
public class DataIntegrityStatusResponseDto
{
    /// <summary>
    /// Count of orphaned users currently detected.
    /// </summary>
    public int orphanedUserCount { get; set; }

    /// <summary>
    /// Total count of soft-delete inconsistencies across all user profile tables.
    /// </summary>
    public int totalSoftDeleteInconsistencies { get; set; }

    /// <summary>
    /// Soft-delete inconsistency counts grouped by profile type.
    /// </summary>
    public SoftDeleteInconsistenciesResponseDto softDeleteInconsistencies { get; set; } = new();

    /// <summary>
    /// Timestamp of the most recent integrity evaluation.
    /// </summary>
    public DateTime? checkedAt { get; set; }

    /// <summary>
    /// Raw integrity flag reported by the cleanup service.
    /// </summary>
    public bool? isHealthy { get; set; }

    /// <summary>
    /// Health status for the data-integrity dependency.
    /// </summary>
    public string? status { get; set; }

    /// <summary>
    /// Error details when the integrity evaluation fails.
    /// </summary>
    public string? error { get; set; }
}

/// <summary>
/// Represents soft-delete inconsistency counts per profile type.
/// </summary>
public class SoftDeleteInconsistenciesResponseDto
{
    /// <summary>
    /// Student profile inconsistency count.
    /// </summary>
    public int students { get; set; }

    /// <summary>
    /// Instructor profile inconsistency count.
    /// </summary>
    public int instructors { get; set; }

    /// <summary>
    /// Admin profile inconsistency count.
    /// </summary>
    public int admins { get; set; }
}
