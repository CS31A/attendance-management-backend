using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute()
        : base("{0} must not be empty.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value is Guid guid && guid != Guid.Empty;
    }
}
