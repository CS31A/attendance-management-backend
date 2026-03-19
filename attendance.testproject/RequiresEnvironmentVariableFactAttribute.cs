namespace attendance.testproject;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresEnvironmentVariableFactAttribute : FactAttribute
{
    public RequiresEnvironmentVariableFactAttribute(string environmentVariableName)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(environmentVariableName)))
        {
            Skip = $"Set {environmentVariableName} to run this test.";
        }
    }
}
