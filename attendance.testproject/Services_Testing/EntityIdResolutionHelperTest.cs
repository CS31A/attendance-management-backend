using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;

namespace attendance.testproject.Services_Testing;

public class EntityIdResolutionHelperTest
{
    [Fact]
    public void RequireGuid_WithValidGuid_ReturnsGuid()
    {
        var id = Guid.NewGuid();
        var result = EntityIdResolutionHelper.RequireGuid(id, "Section");
        Assert.Equal(id, result);
    }

    [Fact]
    public void RequireGuid_WithNull_ThrowsValidationException()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            EntityIdResolutionHelper.RequireGuid(null, "Student"));

        Assert.Equal("Student reference is required.", exception.Message);
    }

    [Fact]
    public void RequireGuid_WithEmptyGuid_ThrowsValidationException()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            EntityIdResolutionHelper.RequireGuid(Guid.Empty, "Subject"));

        Assert.Equal("Subject reference is required.", exception.Message);
    }
}
