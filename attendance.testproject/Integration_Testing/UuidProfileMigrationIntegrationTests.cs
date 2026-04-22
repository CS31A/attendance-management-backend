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
                Firstname = "Uuid",
                Lastname = "Student",
                IsRegular = true,
                SectionId = scenario.PrimarySectionId,
                CreatedAt = now,
                UpdatedAt = now
            };

            var instructor = new Instructor
            {
                UserId = instructorUser.Id,
                Firstname = "Uuid",
                Lastname = "Instructor",
                CreatedAt = now,
                UpdatedAt = now
            };

            var admin = new Admin
            {
                UserId = adminUser.Id,
                Firstname = "Uuid",
                Lastname = "Admin",
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Students.Add(student);
            dbContext.Instructors.Add(instructor);
            dbContext.Admins.Add(admin);

            await dbContext.SaveChangesAsync(cancellationToken);

            var insertedStudentUuid = student.Uuid;
            var insertedInstructorUuid = instructor.Uuid;
            var insertedAdminUuid = admin.Uuid;

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

        Assert.Equal(persisted.InsertedStudentUuid, persisted.Student.Uuid);
        Assert.Equal(persisted.InsertedInstructorUuid, persisted.Instructor.Uuid);
        Assert.Equal(persisted.InsertedAdminUuid, persisted.Admin.Uuid);
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
                StudentUuid = student.Uuid,
                EnrollmentId = persistedEnrollment.Id,
                EnrollmentStudentId = persistedEnrollment.StudentId,
                EnrollmentStudentUuid = persistedEnrollment.Student.Uuid,
                EnrollmentSubjectId = persistedEnrollment.SubjectId,
                EnrollmentSectionId = persistedEnrollment.SectionId,
                AdditionalEnrollmentIds = studentWithEnrollments.AdditionalEnrollments.Select(row => row.Id).ToList()
            };
        });

        Assert.True(persisted.StudentId > 0);
        Assert.NotEqual(Guid.Empty, persisted.StudentUuid);
        Assert.Equal(persisted.StudentId, persisted.EnrollmentStudentId);
        Assert.Equal(persisted.StudentUuid, persisted.EnrollmentStudentUuid);
        Assert.True(persisted.EnrollmentSubjectId > 0);
        Assert.True(persisted.EnrollmentSectionId > 0);
        Assert.Contains(persisted.EnrollmentId, persisted.AdditionalEnrollmentIds);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task ExpandedPhase8UuidTables_HaveMigrationCoverage_ForSliceAAndSliceB()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();

        var tableStates = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
        {
            return new[]
            {
                await ReadUuidTableStateAsync(dbContext, "Courses", cancellationToken),
                await ReadUuidTableStateAsync(dbContext, "Sections", cancellationToken),
                await ReadUuidTableStateAsync(dbContext, "StudentEnrollments", cancellationToken),
                await ReadUuidTableStateAsync(dbContext, "Sessions", cancellationToken),
                await ReadUuidTableStateAsync(dbContext, "AttendanceRecords", cancellationToken),
                await ReadUuidTableStateAsync(dbContext, "QrCodes", cancellationToken)
            };
        });

        Assert.Collection(
            tableStates,
            state => AssertUuidTableState(state, "Courses", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "Sections", minimumExpectedRows: 2),
            state => AssertUuidTableState(state, "StudentEnrollments", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "Sessions", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "AttendanceRecords", minimumExpectedRows: 1),
            state => AssertUuidTableState(state, "QrCodes", minimumExpectedRows: 1));
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
    SUM(CASE WHEN [Uuid] IS NULL THEN 1 ELSE 0 END) AS NullUuidRows,
    SUM(CASE WHEN [Uuid] = '{ZeroUuidLiteral}' THEN 1 ELSE 0 END) AS ZeroUuidRows,
    COUNT(*) - COUNT(DISTINCT [Uuid]) AS DuplicateUuidRows
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
}
