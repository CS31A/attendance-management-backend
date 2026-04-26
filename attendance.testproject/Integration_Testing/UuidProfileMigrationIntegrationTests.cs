using System.Data;
using System.Data.Common;
using attendance.testproject.Integration_Testing.Support;
using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Integration_Testing;

public sealed class UuidProfileMigrationIntegrationTests
{
    private static readonly string ZeroUuidLiteral = Guid.Empty.ToString();

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task ExistingSeededWave1Profiles_HaveBackfilledUniqueNonZeroUuidValues()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();

        var tableStates = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
        {
            return new[]
            {
                await ReadUuidTableStateAsync(dbContext, "Students", cancellationToken),
                await ReadUuidTableStateAsync(dbContext, "Instructors", cancellationToken),
                await ReadUuidTableStateAsync(dbContext, "Admins", cancellationToken)
            };
        });

        Assert.Collection(
            tableStates,
            state => AssertUuidTableState(state, "Students", minimumExpectedRows: 3),
            state => AssertUuidTableState(state, "Instructors", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "Admins", minimumExpectedRows: 1));
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task NewWave1Profiles_GetDatabaseGeneratedUuids_AndUpdatesPreserveThem()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();

        var persisted = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
        {
            var scenario = host.AdminUserManagementScenario ?? throw new InvalidOperationException("Admin user management scenario was not loaded.");
            var suffix = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;

            var studentUser = CreateIdentityUser($"uuid-student-{suffix}");
            var instructorUser = CreateIdentityUser($"uuid-instructor-{suffix}");
            var adminUser = CreateIdentityUser($"uuid-admin-{suffix}");

            dbContext.Users.AddRange(studentUser, instructorUser, adminUser);

            var student = new Student
            {
                UserId = studentUser.Id,
                Firstname = "Id",
                Lastname = "Student",
                IsRegular = true,
                SectionId = scenario.PrimarySectionId,
                Usn = $"UUID-TEST-{suffix}",
                CreatedAt = now,
                UpdatedAt = now
            };

            var instructor = new Instructor
            {
                UserId = instructorUser.Id,
                Firstname = "Id",
                Lastname = "Instructor",
                CreatedAt = now,
                UpdatedAt = now
            };

            var admin = new Admin
            {
                UserId = adminUser.Id,
                Firstname = "Id",
                Lastname = "Admin",
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Students.Add(student);
            dbContext.Instructors.Add(instructor);
            dbContext.Admins.Add(admin);

            await dbContext.SaveChangesAsync(cancellationToken);

            var insertedStudentUuid = student.Id;
            var insertedInstructorUuid = instructor.Id;
            var insertedAdminUuid = admin.Id;

            student.Lastname = "Student Updated";
            student.UpdatedAt = now.AddMinutes(1);
            instructor.Lastname = "Instructor Updated";
            instructor.UpdatedAt = now.AddMinutes(1);
            admin.Lastname = "Admin Updated";
            admin.UpdatedAt = now.AddMinutes(1);

            await dbContext.SaveChangesAsync(cancellationToken);

            return new
            {
                Student = await dbContext.Students.AsNoTracking().SingleAsync(row => row.Id == student.Id, cancellationToken),
                Instructor = await dbContext.Instructors.AsNoTracking().SingleAsync(row => row.Id == instructor.Id, cancellationToken),
                Admin = await dbContext.Admins.AsNoTracking().SingleAsync(row => row.Id == admin.Id, cancellationToken),
                InsertedStudentUuid = insertedStudentUuid,
                InsertedInstructorUuid = insertedInstructorUuid,
                InsertedAdminUuid = insertedAdminUuid
            };
        });

        Assert.NotEqual(Guid.Empty, persisted.InsertedStudentUuid);
        Assert.NotEqual(Guid.Empty, persisted.InsertedInstructorUuid);
        Assert.NotEqual(Guid.Empty, persisted.InsertedAdminUuid);

        Assert.Equal(persisted.InsertedStudentUuid, persisted.Student.Id);
        Assert.Equal(persisted.InsertedInstructorUuid, persisted.Instructor.Id);
        Assert.Equal(persisted.InsertedAdminUuid, persisted.Admin.Id);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task StudentEnrollment_IntForeignKeyPathsRemainCoherent_AfterUuidMigration()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();

        var persisted = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
        {
            var scenario = host.AdminUserManagementScenario ?? throw new InvalidOperationException("Admin user management scenario was not loaded.");
            var now = DateTime.UtcNow;
            var student = await dbContext.Students.SingleAsync(row => row.UserId == scenario.ActiveStudentUserId, cancellationToken);
            var codeSuffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

            var subject = new Subject
            {
                Name = $"UUID Migration Subject {codeSuffix}",
                Code = $"UUID-{codeSuffix}",
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Subjects.Add(subject);
            await dbContext.SaveChangesAsync(cancellationToken);

            var enrollment = new StudentEnrollment
            {
                StudentId = student.Id,
                SectionId = scenario.PrimarySectionId,
                SubjectId = subject.Id,
                EnrollmentType = "Irregular",
                AcademicYear = "2025-2026",
                Semester = "2nd",
                EnrolledAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.StudentEnrollments.Add(enrollment);
            await dbContext.SaveChangesAsync(cancellationToken);

            var persistedEnrollment = await dbContext.StudentEnrollments
                .AsNoTracking()
                .Include(row => row.Student)
                .Include(row => row.Subject)
                .SingleAsync(row => row.Id == enrollment.Id, cancellationToken);

            var studentWithEnrollments = await dbContext.Students
                .AsNoTracking()
                .Include(row => row.AdditionalEnrollments)
                .SingleAsync(row => row.Id == student.Id, cancellationToken);

            return new
            {
                StudentId = student.Id,
                StudentUuid = student.Id,
                EnrollmentId = persistedEnrollment.Id,
                EnrollmentStudentId = persistedEnrollment.StudentId,
                EnrollmentStudentUuid = persistedEnrollment.Student.Id,
                EnrollmentSubjectId = persistedEnrollment.SubjectId,
                EnrollmentSectionId = persistedEnrollment.SectionId,
                AdditionalEnrollmentIds = studentWithEnrollments.AdditionalEnrollments.Select(row => row.Id).ToList()
            };
        });

        Assert.NotEqual(Guid.Empty, persisted.StudentId);
        Assert.NotEqual(Guid.Empty, persisted.StudentUuid);
        Assert.Equal(persisted.StudentId, persisted.EnrollmentStudentId);
        Assert.Equal(persisted.StudentUuid, persisted.EnrollmentStudentUuid);
        Assert.NotEqual(Guid.Empty, persisted.EnrollmentSubjectId);
        Assert.NotEqual(Guid.Empty, persisted.EnrollmentSectionId);
        Assert.Contains(persisted.EnrollmentId, persisted.AdditionalEnrollmentIds);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task ExpandedPhase8UuidTables_HaveMigrationCoverage_ForSliceAAndSliceBAndFingerprintSupport()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();

        var persisted = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
        {
            var scenario = host.AdminUserManagementScenario ?? throw new InvalidOperationException("Admin user management scenario was not loaded.");
            var now = DateTime.UtcNow;
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

            var student = await dbContext.Students.SingleAsync(row => row.UserId == scenario.ActiveStudentUserId, cancellationToken);
            var instructor = await dbContext.Instructors.SingleAsync(row => row.UserId == scenario.ActiveInstructorUserId, cancellationToken);

            var subject = new Subject
            {
                Name = $"UUID Migration Subject {suffix}",
                Code = $"PH8-{suffix}",
                CreatedAt = now,
                UpdatedAt = now
            };
            var classroom = new Classroom
            {
                Name = $"UUID Migration Room {suffix}",
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Subjects.Add(subject);
            dbContext.Classrooms.Add(classroom);
            await dbContext.SaveChangesAsync(cancellationToken);

            var schedule = new Schedules
            {
                SubjectId = subject.Id,
                ClassroomId = classroom.Id,
                SectionId = scenario.PrimarySectionId,
                InstructorId = instructor.Id,
                DayOfWeek = "Monday",
                TimeIn = new TimeOnly(9, 0),
                TimeOut = new TimeOnly(10, 0),
                CreatedAt = now,
                UpdatedAt = now
            };
            var enrollment = new StudentEnrollment
            {
                StudentId = student.Id,
                SectionId = scenario.PrimarySectionId,
                SubjectId = subject.Id,
                EnrollmentType = "Irregular",
                AcademicYear = "2025-2026",
                Semester = "2nd",
                EnrolledAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Schedules.Add(schedule);
            dbContext.StudentEnrollments.Add(enrollment);
            await dbContext.SaveChangesAsync(cancellationToken);

            var session = new Session
            {
                ScheduleId = schedule.Id,
                Status = "InProgress",
                SessionDate = now.Date,
                ActualStartTime = now,
                AttendanceCutOff = now.AddMinutes(15),
                StartedBy = instructor.Id,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Sessions.Add(session);
            await dbContext.SaveChangesAsync(cancellationToken);

            var qrCode = new QrCode
            {
                SessionId = session.Id,
                QrHash = $"phase8-uuid-{suffix}",
                GeneratedAt = now,
                ExpiresAt = now.AddMinutes(15),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.QrCodes.Add(qrCode);
            await dbContext.SaveChangesAsync(cancellationToken);

            var attendanceRecord = new AttendanceRecord
            {
                StudentId = student.Id,
                SessionId = session.Id,
                QrCodeId = qrCode.Id,
                CheckInTime = now.AddMinutes(1),
                Status = "Present",
                IsManualEntry = false,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.AttendanceRecords.Add(attendanceRecord);
            await dbContext.SaveChangesAsync(cancellationToken);

            var device = new FingerprintDevice
            {
                DeviceIdentifier = $"device-{suffix}",
                Name = $"UUID Device {suffix}",
                Location = "Integration Lab",
                IsActive = true,
                LastSeenAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.FingerprintDevices.Add(device);
            await dbContext.SaveChangesAsync(cancellationToken);

            var fingerprint = new Fingerprint
            {
                UserId = student.UserId,
                TemplateData = $"template-{suffix}",
                DeviceId = device.DeviceIdentifier,
                SensorFingerprintId = 7,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            };

            var enrollmentSession = new FingerprintEnrollmentSession
            {
                DeviceId = device.Id,
                StudentId = student.Id,
                RequestedByUserId = scenario.ActiveInstructorUserId,
                AssignedSensorFingerprintId = fingerprint.SensorFingerprintId,
                Status = "Completed",
                ExpiresAt = now.AddMinutes(10),
                StartedAt = now,
                CompletedAt = now.AddMinutes(1),
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Fingerprints.Add(fingerprint);
            dbContext.FingerprintEnrollmentSessions.Add(enrollmentSession);
            await dbContext.SaveChangesAsync(cancellationToken);

            var scanEvent = new FingerprintScanEvent
            {
                DeviceId = device.Id,
                MatchedStudentId = student.Id,
                SessionId = session.Id,
                AttendanceRecordId = attendanceRecord.Id,
                MatchScore = 0.9750m,
                ThresholdUsed = 0.8000m,
                Status = "Matched",
                PayloadHash = $"payload-{suffix}",
                CapturedAt = now.AddMinutes(2),
                ReceivedAt = now.AddMinutes(2),
                CreatedAt = now.AddMinutes(2)
            };

            dbContext.FingerprintScanEvents.Add(scanEvent);
            await dbContext.SaveChangesAsync(cancellationToken);

            var persistedEnrollment = await dbContext.StudentEnrollments
                .AsNoTracking()
                .Include(row => row.Student)
                .Include(row => row.Section)
                .Include(row => row.Subject)
                .SingleAsync(row => row.Id == enrollment.Id, cancellationToken);
            var persistedSession = await dbContext.Sessions
                .AsNoTracking()
                .Include(row => row.Schedule)
                .ThenInclude(row => row.Subject)
                .Include(row => row.Schedule)
                .ThenInclude(row => row.Section)
                .Include(row => row.Schedule)
                .ThenInclude(row => row.Classroom)
                .SingleAsync(row => row.Id == session.Id, cancellationToken);
            var persistedQrCode = await dbContext.QrCodes
                .AsNoTracking()
                .Include(row => row.Session)
                .SingleAsync(row => row.Id == qrCode.Id, cancellationToken);
            var persistedAttendance = await dbContext.AttendanceRecords
                .AsNoTracking()
                .Include(row => row.Student)
                .Include(row => row.Session)
                .Include(row => row.QrCode)
                .SingleAsync(row => row.Id == attendanceRecord.Id, cancellationToken);
            var persistedFingerprint = await dbContext.Fingerprints
                .AsNoTracking()
                .SingleAsync(row => row.Id == fingerprint.Id, cancellationToken);
            var persistedDevice = await dbContext.FingerprintDevices
                .AsNoTracking()
                .SingleAsync(row => row.Id == device.Id, cancellationToken);
            var persistedEnrollmentSession = await dbContext.FingerprintEnrollmentSessions
                .AsNoTracking()
                .Include(row => row.Device)
                .Include(row => row.Student)
                .SingleAsync(row => row.Id == enrollmentSession.Id, cancellationToken);
            var persistedScanEvent = await dbContext.FingerprintScanEvents
                .AsNoTracking()
                .Include(row => row.Device)
                .Include(row => row.MatchedStudent)
                .Include(row => row.Session)
                .Include(row => row.AttendanceRecord)
                .SingleAsync(row => row.Id == scanEvent.Id, cancellationToken);

            return new ExpandedPhase8UuidPersistence(
                new[]
                {
                    await ReadUuidTableStateAsync(dbContext, "Courses", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "Subjects", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "Sections", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "Classrooms", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "Schedules", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "StudentEnrollments", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "Sessions", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "AttendanceRecords", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "QrCodes", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "Fingerprints", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "FingerprintDevices", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "FingerprintEnrollmentSessions", cancellationToken),
                    await ReadUuidTableStateAsync(dbContext, "FingerprintScanEvents", cancellationToken)
                },
                persistedEnrollment,
                persistedSession,
                persistedQrCode,
                persistedAttendance,
                persistedFingerprint,
                persistedDevice,
                persistedEnrollmentSession,
                persistedScanEvent);
        });

        Assert.Collection(
            persisted.TableStates,
            state => AssertUuidTableState(state, "Courses", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "Subjects", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "Sections", minimumExpectedRows: 2),
            state => AssertUuidTableState(state, "Classrooms", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "Schedules", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "StudentEnrollments", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "Sessions", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "AttendanceRecords", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "QrCodes", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "Fingerprints", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "FingerprintDevices", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "FingerprintEnrollmentSessions", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "FingerprintScanEvents", minimumExpectedRows: 1));

        Assert.NotEqual(Guid.Empty, persisted.Enrollment.Id);
        Assert.Equal(persisted.Enrollment.StudentId, persisted.Attendance.StudentId);
        Assert.Equal(persisted.Enrollment.Student.Id, persisted.Attendance.Student.Id);
        Assert.Equal(persisted.Enrollment.SectionId, persisted.Enrollment.Section.Id);
        Assert.NotEqual(Guid.Empty, persisted.Enrollment.Section.Id);
        Assert.Equal(persisted.Enrollment.SubjectId, persisted.Enrollment.Subject.Id);
        Assert.NotEqual(Guid.Empty, persisted.Enrollment.Subject.Id);

        Assert.NotEqual(Guid.Empty, persisted.Session.Id);
        Assert.Equal(persisted.Session.ScheduleId, persisted.Session.Schedule.Id);
        Assert.Equal(persisted.Session.Schedule.SectionId, persisted.Enrollment.SectionId);
        Assert.Equal(persisted.Session.Schedule.SubjectId, persisted.Enrollment.SubjectId);
        Assert.NotEqual(Guid.Empty, persisted.Session.Schedule.ClassroomId);
        Assert.NotEqual(Guid.Empty, persisted.Session.Schedule.Id);
        Assert.NotEqual(Guid.Empty, persisted.Session.Schedule.Section.Id);
        Assert.NotEqual(Guid.Empty, persisted.Session.Schedule.Subject.Id);
        Assert.NotEqual(Guid.Empty, persisted.Session.Schedule.Classroom.Id);

        Assert.NotEqual(Guid.Empty, persisted.QrCode.Id);
        Assert.Equal(persisted.Session.Id, persisted.QrCode.SessionId);
        Assert.Equal(persisted.Session.Id, persisted.QrCode.Session.Id);

        Assert.NotEqual(Guid.Empty, persisted.Attendance.Id);
        Assert.Equal(persisted.Attendance.StudentId, persisted.Enrollment.StudentId);
        Assert.Equal(persisted.Session.Id, persisted.Attendance.SessionId);
        Assert.Equal(persisted.Session.Id, persisted.Attendance.Session.Id);
        Assert.Equal(persisted.QrCode.Id, persisted.Attendance.QrCodeId);
        Assert.Equal(persisted.QrCode.Id, persisted.Attendance.QrCode!.Id);

        Assert.NotEqual(Guid.Empty, persisted.Fingerprint.Id);
        Assert.Equal(persisted.FingerprintDevice.DeviceIdentifier, persisted.Fingerprint.DeviceId);
        Assert.True(persisted.Fingerprint.SensorFingerprintId > 0);

        Assert.NotEqual(Guid.Empty, persisted.FingerprintDevice.Id);
        Assert.Equal(persisted.FingerprintScanEvent.DeviceId, persisted.FingerprintDevice.Id);
        Assert.Equal(persisted.FingerprintEnrollmentSession.DeviceId, persisted.FingerprintDevice.Id);

        Assert.NotEqual(Guid.Empty, persisted.FingerprintEnrollmentSession.Id);
        Assert.NotEqual(persisted.FingerprintEnrollmentSession.Id, persisted.FingerprintEnrollmentSession.EnrollmentSessionId);
        Assert.NotEqual(Guid.Empty, persisted.FingerprintEnrollmentSession.EnrollmentSessionId);
        Assert.Equal(persisted.FingerprintEnrollmentSession.DeviceId, persisted.FingerprintEnrollmentSession.Device.Id);
        Assert.Equal(persisted.FingerprintEnrollmentSession.StudentId, persisted.FingerprintEnrollmentSession.Student.Id);
        Assert.Equal(persisted.Enrollment.StudentId, persisted.FingerprintEnrollmentSession.StudentId);

        Assert.NotEqual(Guid.Empty, persisted.FingerprintScanEvent.Id);
        Assert.NotEqual(persisted.FingerprintScanEvent.Id, persisted.FingerprintScanEvent.EventId);
        Assert.NotEqual(Guid.Empty, persisted.FingerprintScanEvent.EventId);
        Assert.Equal(persisted.FingerprintDevice.Id, persisted.FingerprintScanEvent.DeviceId);
        Assert.Equal(persisted.Enrollment.StudentId, persisted.FingerprintScanEvent.MatchedStudentId);
        Assert.Equal(persisted.Session.Id, persisted.FingerprintScanEvent.SessionId);
        Assert.Equal(persisted.Attendance.Id, persisted.FingerprintScanEvent.AttendanceRecordId);
        Assert.Equal(persisted.FingerprintDevice.Id, persisted.FingerprintScanEvent.Device.Id);
        Assert.Equal(persisted.Attendance.Id, persisted.FingerprintScanEvent.AttendanceRecord!.Id);
    }

    private static void AssertUuidTableState(UuidTableState state, string expectedTableName, int minimumExpectedRows)
    {
        Assert.Equal(expectedTableName, state.TableName);
        Assert.True(state.TotalRows >= minimumExpectedRows, $"Expected at least {minimumExpectedRows} rows in {state.TableName} but found {state.TotalRows}.");
        Assert.Equal(0, state.NullUuidRows);
        Assert.Equal(0, state.DuplicateUuidRows);
        Assert.Equal(0, state.ZeroUuidRows);
    }

    private static IdentityUser CreateIdentityUser(string key)
    {
        var email = $"{key}@integration.test";
        return new IdentityUser
        {
            Id = key,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
    }

    private static async Task<UuidTableState> ReadUuidTableStateAsync(
        ApplicationDbContext dbContext,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = $@"
SELECT
    COUNT(*) AS TotalRows,
    COALESCE(SUM(CASE WHEN [Id] IS NULL THEN 1 ELSE 0 END), 0) AS NullUuidRows,
    COALESCE(SUM(CASE WHEN [Id] = '{ZeroUuidLiteral}' THEN 1 ELSE 0 END), 0) AS ZeroUuidRows,
    COUNT(*) - COUNT(DISTINCT [Id]) AS DuplicateUuidRows
FROM [{tableName}];";

        if (command.Connection is null)
        {
            throw new InvalidOperationException($"No database connection available for table {tableName}.");
        }

        var shouldCloseConnection = command.Connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            Assert.True(await reader.ReadAsync(cancellationToken));

            return new UuidTableState(
                tableName,
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetInt32(3));
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await command.Connection.CloseAsync();
            }
        }
    }

    private sealed record UuidTableState(
        string TableName,
        int TotalRows,
        int NullUuidRows,
        int ZeroUuidRows,
        int DuplicateUuidRows);

    private sealed record ExpandedPhase8UuidPersistence(
        UuidTableState[] TableStates,
        StudentEnrollment Enrollment,
        Session Session,
        QrCode QrCode,
        AttendanceRecord Attendance,
        Fingerprint Fingerprint,
        FingerprintDevice FingerprintDevice,
        FingerprintEnrollmentSession FingerprintEnrollmentSession,
        FingerprintScanEvent FingerprintScanEvent);
}
