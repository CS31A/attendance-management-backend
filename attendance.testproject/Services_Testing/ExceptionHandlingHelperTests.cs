using attendance_monitoring.Helpers;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Services_Testing;

public class ExceptionHandlingHelperTests
{
    [Fact]
    public void IsUniqueConstraintViolation_WithQrHashConstraintHint_ReturnsTrueForQrHashConstraint()
    {
        var ex = new DbUpdateException(
            "Error",
            new Exception("Violation of UNIQUE KEY constraint 'IX_QrCodes_QrHash'. Cannot insert duplicate key."));

        var result = ExceptionHandlingHelper.IsUniqueConstraintViolation(ex, "IX_QrCodes_QrHash", "QrCodes.QrHash", "QrHash");

        Assert.True(result);
    }

    [Fact]
    public void IsUniqueConstraintViolation_WithQrHashConstraintHint_ReturnsFalseForOtherUniqueConstraint()
    {
        var ex = new DbUpdateException(
            "Error",
            new Exception("UNIQUE constraint failed: AttendanceRecords.StudentId, AttendanceRecords.SessionId"));

        var result = ExceptionHandlingHelper.IsUniqueConstraintViolation(ex, "IX_QrCodes_QrHash", "QrCodes.QrHash", "QrHash");

        Assert.False(result);
    }
}
