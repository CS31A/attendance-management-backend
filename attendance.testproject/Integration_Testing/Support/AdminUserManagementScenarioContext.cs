namespace attendance.testproject.Integration_Testing.Support;

internal sealed class AdminUserManagementScenarioContext
{
    public required string AdminUserId { get; init; }
    public required string AdminEmail { get; init; }
    public required string ActiveStudentUserId { get; init; }
    public required string ActiveStudentEmail { get; init; }
    public required string ArchivedStudentUserId { get; init; }
    public required string ArchivedStudentEmail { get; init; }
    public required string ActiveInstructorUserId { get; init; }
    public required string ActiveInstructorEmail { get; init; }
    public required string ConflictStudentUserId { get; init; }
    public required string ConflictStudentEmail { get; init; }
    public required string OrphanedUserId { get; init; }
    public required string OrphanedUserEmail { get; init; }
    public required int PrimarySectionId { get; init; }
    public required int AlternateSectionId { get; init; }
    public required int ActiveStudentRefreshTokenId { get; init; }
    public required int ControlRefreshTokenId { get; init; }
}
