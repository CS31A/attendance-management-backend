namespace attendance.testproject.Integration_Testing.Support;

internal sealed class AccountScenarioContext
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public int? RoleSpecificId { get; init; }
}
