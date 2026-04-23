namespace attendance.testproject.Integration_Testing.Support;

/// <summary>
/// Context for instructor sections integration test scenario.
/// </summary>
internal sealed class InstructorScenarioContext
{
    public required int InstructorId { get; init; }
    public required string InstructorUserId { get; init; }
    public required string InstructorUsername { get; init; }
    public required string InstructorFirstname { get; init; }
    public required string InstructorLastname { get; init; }
    public required int InstructorWithNoSchedulesId { get; init; }
    public required string InstructorWithNoSchedulesUserId { get; init; }
    public required string InstructorWithNoSchedulesUsername { get; init; }
}
