using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;

namespace attendance.testproject.Services_Testing;

public class EntityIdResolutionHelperTest
{
    [Fact]
    public async Task ResolveEntityIdAsync_WithConflictingIdentifiers_ThrowsValidationException()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            EntityIdResolutionHelper.ResolveEntityIdAsync(
                1,
                Guid.NewGuid(),
                "Section",
                id => Task.FromResult<int?>(id),
                _ => Task.FromResult<int?>(2)));

        Assert.Equal("Conflicting Section identifiers were provided.", exception.Message);
    }

    [Fact]
    public async Task ResolveEntityIdAsync_WithoutIdentifiers_ThrowsValidationException()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            EntityIdResolutionHelper.ResolveEntityIdAsync(
                null,
                null,
                "Student",
                id => Task.FromResult<int?>(id),
                _ => Task.FromResult<int?>(1)));

        Assert.Equal("Student reference is required.", exception.Message);
    }

    [Fact]
    public async Task ResolveEntityIdAsync_WithUuidOnly_ReturnsResolvedIdentifier()
    {
        var resolvedId = await EntityIdResolutionHelper.ResolveEntityIdAsync(
            null,
            Guid.NewGuid(),
            "Subject",
            id => Task.FromResult<int?>(id),
            _ => Task.FromResult<int?>(7));

        Assert.Equal(7, resolvedId);
    }
}
