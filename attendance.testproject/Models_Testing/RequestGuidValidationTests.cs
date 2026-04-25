using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Models.DTO.Request;

namespace attendance.testproject.Models_Testing;

public class RequestGuidValidationTests
{
    [Theory]
    [MemberData(nameof(RequestsWithEmptyGuid))]
    public void RequestDtos_WithEmptyGuid_AreInvalid(object request, string memberName)
    {
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            results,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, result => result.MemberNames.Contains(memberName));
    }

    public static IEnumerable<object[]> RequestsWithEmptyGuid()
    {
        yield return [new CreateStudentEnrollment { StudentId = Guid.Empty, SectionId = Guid.NewGuid(), SubjectId = Guid.NewGuid() }, nameof(CreateStudentEnrollment.StudentId)];
        yield return [new CreateStudentEnrollment { StudentId = Guid.NewGuid(), SectionId = Guid.Empty, SubjectId = Guid.NewGuid() }, nameof(CreateStudentEnrollment.SectionId)];
        yield return [new CreateStudentEnrollment { StudentId = Guid.NewGuid(), SectionId = Guid.NewGuid(), SubjectId = Guid.Empty }, nameof(CreateStudentEnrollment.SubjectId)];
        yield return [new CreateSchedule { DayOfWeek = "Monday", SubjectId = Guid.Empty, ClassroomId = Guid.NewGuid(), SectionId = Guid.NewGuid(), InstructorId = Guid.NewGuid(), TimeIn = new TimeOnly(8, 0), TimeOut = new TimeOnly(9, 0) }, nameof(CreateSchedule.SubjectId)];
        yield return [new CreateSchedule { DayOfWeek = "Monday", SubjectId = Guid.NewGuid(), ClassroomId = Guid.Empty, SectionId = Guid.NewGuid(), InstructorId = Guid.NewGuid(), TimeIn = new TimeOnly(8, 0), TimeOut = new TimeOnly(9, 0) }, nameof(CreateSchedule.ClassroomId)];
        yield return [new CreateSchedule { DayOfWeek = "Monday", SubjectId = Guid.NewGuid(), ClassroomId = Guid.NewGuid(), SectionId = Guid.Empty, InstructorId = Guid.NewGuid(), TimeIn = new TimeOnly(8, 0), TimeOut = new TimeOnly(9, 0) }, nameof(CreateSchedule.SectionId)];
        yield return [new CreateSchedule { DayOfWeek = "Monday", SubjectId = Guid.NewGuid(), ClassroomId = Guid.NewGuid(), SectionId = Guid.NewGuid(), InstructorId = Guid.Empty, TimeIn = new TimeOnly(8, 0), TimeOut = new TimeOnly(9, 0) }, nameof(CreateSchedule.InstructorId)];
        yield return [new StartFingerprintEnrollmentSessionRequest { StudentId = Guid.Empty, DeviceId = "sensor-1" }, nameof(StartFingerprintEnrollmentSessionRequest.StudentId)];
        yield return [new CompleteFingerprintEnrollmentRequest { Id = Guid.Empty, DeviceId = "sensor-1", SensorFingerprintId = 1, Success = true }, nameof(CompleteFingerprintEnrollmentRequest.Id)];
        yield return [new CreateQrCode { SessionId = Guid.Empty }, nameof(CreateQrCode.SessionId)];
        yield return [new QrCodeRequest { SessionId = Guid.Empty }, nameof(QrCodeRequest.SessionId)];
    }
}
