using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Extensions.WebApplicationExtensions;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace attendance.testproject.Integration_Testing;

public sealed class StartupInitializationIntegrationTests
{
    [Fact]
    public async Task InitializeApplicationAsync_WithInMemoryDatabase_DoesNotSeed()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"startup-init-{Guid.NewGuid():N}"));

        var seeder = new CountingSeederService();
        builder.Services.AddSingleton<IDataSeederService>(seeder);

        await using var app = builder.Build();

        await app.InitializeApplicationAsync();

        // Seeder is intentionally disabled in InitializeApplicationAsync
        // to prevent automatic seeding in production environments.
        // Development databases are seeded via migration scripts with survey data.
        Assert.Equal(0, seeder.CallCount);
    }

    [Fact]
    public async Task ApplicationDbContext_WithSqlite_GeneratesProfileUuidsWithoutSqlServerDefaults()
    {
        await using var connection = new SqliteConnection($"Data Source=file:uuid-sqlite-{Guid.NewGuid():N}?mode=memory&cache=shared");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var now = DateTime.UtcNow;
        var course = new Course
        {
            Name = $"Course-{Guid.NewGuid():N}",
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var section = new Section
        {
            Name = $"Section-{Guid.NewGuid():N}",
            CourseId = course.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.Sections.Add(section);
        await context.SaveChangesAsync();

        var studentUser = new IdentityUser
        {
            Id = $"student-{Guid.NewGuid():N}",
            UserName = "student@test.local",
            NormalizedUserName = "STUDENT@TEST.LOCAL",
            Email = "student@test.local",
            NormalizedEmail = "STUDENT@TEST.LOCAL"
        };
        var instructorUser = new IdentityUser
        {
            Id = $"instructor-{Guid.NewGuid():N}",
            UserName = "instructor@test.local",
            NormalizedUserName = "INSTRUCTOR@TEST.LOCAL",
            Email = "instructor@test.local",
            NormalizedEmail = "INSTRUCTOR@TEST.LOCAL"
        };
        var adminUser = new IdentityUser
        {
            Id = $"admin-{Guid.NewGuid():N}",
            UserName = "admin@test.local",
            NormalizedUserName = "ADMIN@TEST.LOCAL",
            Email = "admin@test.local",
            NormalizedEmail = "ADMIN@TEST.LOCAL"
        };

        context.Users.AddRange(studentUser, instructorUser, adminUser);
        context.Students.Add(new Student
        {
            UserId = studentUser.Id,
            Firstname = "Sqlite",
            Lastname = "Student",
            IsRegular = true,
            SectionId = section.Id,
            CreatedAt = now,
            UpdatedAt = now
        });
        context.Instructors.Add(new Instructor
        {
            UserId = instructorUser.Id,
            Firstname = "Sqlite",
            Lastname = "Instructor",
            CreatedAt = now,
            UpdatedAt = now
        });
        context.Admins.Add(new Admin
        {
            UserId = adminUser.Id,
            Firstname = "Sqlite",
            Lastname = "Admin",
            CreatedAt = now,
            UpdatedAt = now
        });

        await context.SaveChangesAsync();

        var student = await context.Students.SingleAsync();
        var instructor = await context.Instructors.SingleAsync();
        var admin = await context.Admins.SingleAsync();

        Assert.NotEqual(Guid.Empty, student.Id);
        Assert.NotEqual(Guid.Empty, instructor.Id);
        Assert.NotEqual(Guid.Empty, admin.Id);
        Assert.NotEqual(student.Id, instructor.Id);
        Assert.NotEqual(student.Id, admin.Id);
        Assert.NotEqual(instructor.Id, admin.Id);
    }

    [Fact]
    public async Task ApplicationDbContext_WithSqlite_EnsureCreated_PersistsWidenedUuidGraphWithoutSqlServerDefaults()
    {
        await using var connection = new SqliteConnection($"Data Source=file:widened-uuid-sqlite-{Guid.NewGuid():N}?mode=memory&cache=shared");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var now = DateTime.UtcNow;
        var course = new Course
        {
            Name = $"Course-{Guid.NewGuid():N}",
            CreatedAt = now,
            UpdatedAt = now
        };
        var subject = new Subject
        {
            Name = $"Subject-{Guid.NewGuid():N}",
            Code = $"SUBJ-{Guid.NewGuid():N}"[..10],
            CreatedAt = now,
            UpdatedAt = now
        };
        var classroom = new Classroom
        {
            Name = $"Room-{Guid.NewGuid():N}",
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Courses.Add(course);
        context.Subjects.Add(subject);
        context.Classrooms.Add(classroom);
        await context.SaveChangesAsync();

        var section = new Section
        {
            Name = $"Section-{Guid.NewGuid():N}",
            CourseId = course.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        var studentUser = new IdentityUser
        {
            Id = $"student-{Guid.NewGuid():N}",
            UserName = "student+widened@test.local",
            NormalizedUserName = "STUDENT+WIDENED@TEST.LOCAL",
            Email = "student+widened@test.local",
            NormalizedEmail = "STUDENT+WIDENED@TEST.LOCAL"
        };
        var instructorUser = new IdentityUser
        {
            Id = $"instructor-{Guid.NewGuid():N}",
            UserName = "instructor+widened@test.local",
            NormalizedUserName = "INSTRUCTOR+WIDENED@TEST.LOCAL",
            Email = "instructor+widened@test.local",
            NormalizedEmail = "INSTRUCTOR+WIDENED@TEST.LOCAL"
        };

        context.Sections.Add(section);
        context.Users.AddRange(studentUser, instructorUser);
        await context.SaveChangesAsync();

        var student = new Student
        {
            UserId = studentUser.Id,
            Firstname = "Widened",
            Lastname = "Student",
            IsRegular = false,
            SectionId = section.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        var instructor = new Instructor
        {
            UserId = instructorUser.Id,
            Firstname = "Widened",
            Lastname = "Instructor",
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Students.Add(student);
        context.Instructors.Add(instructor);
        await context.SaveChangesAsync();

        var schedule = new Schedules
        {
            SubjectId = subject.Id,
            ClassroomId = classroom.Id,
            SectionId = section.Id,
            InstructorId = instructor.Id,
            DayOfWeek = "Monday",
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(9, 0),
            CreatedAt = now,
            UpdatedAt = now
        };
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        var enrollment = new StudentEnrollment
        {
            StudentId = student.Id,
            SectionId = section.Id,
            SubjectId = subject.Id,
            IsActive = true,
            EnrollmentType = "Irregular",
            AcademicYear = "2025-2026",
            Semester = "2nd",
            EnrolledAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.StudentEnrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var session = new Session
        {
            ScheduleId = schedule.Id,
            Status = "Active",
            SessionDate = now.Date,
            ActualStartTime = now,
            AttendanceCutOff = now.AddMinutes(15),
            ActualRoomId = classroom.Id,
            StartedBy = instructor.Id,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = Array.Empty<byte>()
        };
        context.Sessions.Add(session);
        await context.SaveChangesAsync();

        var qrCode = new QrCode
        {
            SessionId = session.Id,
            QrHash = $"qr-{Guid.NewGuid():N}",
            GeneratedAt = now,
            ExpiresAt = now.AddMinutes(10),
            IsActive = true,
            UsageCount = 1,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = Array.Empty<byte>()
        };
        context.QrCodes.Add(qrCode);
        await context.SaveChangesAsync();

        var attendance = new AttendanceRecord
        {
            StudentId = student.Id,
            SessionId = session.Id,
            QrCodeId = qrCode.Id,
            CheckInTime = now.AddMinutes(1),
            Status = "Present",
            CreatedAt = now,
            UpdatedAt = now
        };
        context.AttendanceRecords.Add(attendance);
        await context.SaveChangesAsync();

        var device = new FingerprintDevice
        {
            DeviceIdentifier = $"device-{Guid.NewGuid():N}",
            Name = "SQLite Device",
            Location = "Lab A",
            IsActive = true,
            LastSeenAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.FingerprintDevices.Add(device);
        await context.SaveChangesAsync();

        var enrollmentSession = new FingerprintEnrollmentSession
        {
            DeviceId = device.Id,
            StudentId = student.Id,
            RequestedByUserId = instructorUser.Id,
            AssignedSensorFingerprintId = 17,
            Status = "Completed",
            ExpiresAt = now.AddMinutes(10),
            StartedAt = now,
            CompletedAt = now.AddMinutes(2),
            CreatedAt = now,
            UpdatedAt = now
        };
        context.FingerprintEnrollmentSessions.Add(enrollmentSession);
        await context.SaveChangesAsync();

        var scanEvent = new FingerprintScanEvent
        {
            DeviceId = device.Id,
            MatchedStudentId = student.Id,
            SessionId = session.Id,
            AttendanceRecordId = attendance.Id,
            MatchScore = 0.9876m,
            ThresholdUsed = 0.7500m,
            Status = "Matched",
            PayloadHash = $"payload-{Guid.NewGuid():N}",
            CapturedAt = now.AddMinutes(3),
            ReceivedAt = now.AddMinutes(3),
            CreatedAt = now.AddMinutes(3),
            RowVersion = Array.Empty<byte>()
        };
        context.FingerprintScanEvents.Add(scanEvent);
        await context.SaveChangesAsync();

        var persistedCourse = await context.Courses.AsNoTracking().SingleAsync(row => row.Id == course.Id);
        var persistedSection = await context.Sections.AsNoTracking().SingleAsync(row => row.Id == section.Id);
        var persistedSubject = await context.Subjects.AsNoTracking().SingleAsync(row => row.Id == subject.Id);
        var persistedClassroom = await context.Classrooms.AsNoTracking().SingleAsync(row => row.Id == classroom.Id);
        var persistedSchedule = await context.Schedules.AsNoTracking().SingleAsync(row => row.Id == schedule.Id);
        var persistedEnrollment = await context.StudentEnrollments.AsNoTracking().SingleAsync(row => row.Id == enrollment.Id);
        var persistedSession = await context.Sessions.AsNoTracking().SingleAsync(row => row.Id == session.Id);
        var persistedQrCode = await context.QrCodes.AsNoTracking().SingleAsync(row => row.Id == qrCode.Id);
        var persistedAttendance = await context.AttendanceRecords.AsNoTracking().SingleAsync(row => row.Id == attendance.Id);
        var persistedDevice = await context.FingerprintDevices.AsNoTracking().SingleAsync(row => row.Id == device.Id);
        var persistedEnrollmentSession = await context.FingerprintEnrollmentSessions.AsNoTracking().SingleAsync(row => row.Id == enrollmentSession.Id);
        var persistedScanEvent = await context.FingerprintScanEvents.AsNoTracking().SingleAsync(row => row.Id == scanEvent.Id);

        Assert.NotEqual(Guid.Empty, persistedCourse.Id);
        Assert.NotEqual(Guid.Empty, persistedSection.Id);
        Assert.NotEqual(Guid.Empty, persistedSubject.Id);
        Assert.NotEqual(Guid.Empty, persistedClassroom.Id);
        Assert.NotEqual(Guid.Empty, persistedSchedule.Id);
        Assert.NotEqual(Guid.Empty, persistedEnrollment.Id);
        Assert.NotEqual(Guid.Empty, persistedSession.Id);
        Assert.NotEqual(Guid.Empty, persistedQrCode.Id);
        Assert.NotEqual(Guid.Empty, persistedAttendance.Id);
        Assert.NotEqual(Guid.Empty, persistedDevice.Id);
        Assert.NotEqual(Guid.Empty, persistedEnrollmentSession.Id);
        Assert.NotEqual(Guid.Empty, persistedScanEvent.Id);

        Assert.NotEqual(persistedEnrollmentSession.EnrollmentSessionId, persistedEnrollmentSession.Id);
        Assert.NotEqual(persistedScanEvent.EventId, persistedScanEvent.Id);

        Assert.Equal(course.Id, persistedSection.CourseId);
        Assert.Equal(subject.Id, persistedSchedule.SubjectId);
        Assert.Equal(section.Id, persistedSchedule.SectionId);
        Assert.Equal(classroom.Id, persistedSchedule.ClassroomId);
        Assert.Equal(schedule.Id, persistedSession.ScheduleId);
        Assert.Equal(qrCode.Id, persistedAttendance.QrCodeId);
        Assert.Equal(session.Id, persistedAttendance.SessionId);
        Assert.Equal(student.Id, persistedEnrollment.StudentId);
        Assert.Equal(device.Id, persistedEnrollmentSession.DeviceId);
        Assert.Equal(attendance.Id, persistedScanEvent.AttendanceRecordId);
        Assert.Equal(session.Id, persistedScanEvent.SessionId);
    }

    [Fact]
    public async Task ApplicationDbContext_WithSqlite_AllowsSameScheduleTimeInDifferentClassrooms()
    {
        await using var connection = new SqliteConnection($"Data Source=file:schedule-unique-{Guid.NewGuid():N}?mode=memory&cache=shared");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var now = DateTime.UtcNow;
        var course = new Course
        {
            Name = $"Course-{Guid.NewGuid():N}",
            CreatedAt = now,
            UpdatedAt = now
        };
        var subject = new Subject
        {
            Name = $"Subject-{Guid.NewGuid():N}",
            Code = $"SUBJ-{Guid.NewGuid():N}"[..10],
            CreatedAt = now,
            UpdatedAt = now
        };
        var firstClassroom = new Classroom
        {
            Name = $"Room-A-{Guid.NewGuid():N}",
            CreatedAt = now,
            UpdatedAt = now
        };
        var secondClassroom = new Classroom
        {
            Name = $"Room-B-{Guid.NewGuid():N}",
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Courses.Add(course);
        context.Subjects.Add(subject);
        context.Classrooms.AddRange(firstClassroom, secondClassroom);
        await context.SaveChangesAsync();

        var firstSection = new Section
        {
            Name = $"Section-A-{Guid.NewGuid():N}",
            CourseId = course.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        var secondSection = new Section
        {
            Name = $"Section-B-{Guid.NewGuid():N}",
            CourseId = course.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        var firstInstructorUser = new IdentityUser
        {
            Id = $"instructor-a-{Guid.NewGuid():N}",
            UserName = "instructor-a@test.local",
            NormalizedUserName = "INSTRUCTOR-A@TEST.LOCAL",
            Email = "instructor-a@test.local",
            NormalizedEmail = "INSTRUCTOR-A@TEST.LOCAL"
        };
        var secondInstructorUser = new IdentityUser
        {
            Id = $"instructor-b-{Guid.NewGuid():N}",
            UserName = "instructor-b@test.local",
            NormalizedUserName = "INSTRUCTOR-B@TEST.LOCAL",
            Email = "instructor-b@test.local",
            NormalizedEmail = "INSTRUCTOR-B@TEST.LOCAL"
        };

        context.Sections.AddRange(firstSection, secondSection);
        context.Users.AddRange(firstInstructorUser, secondInstructorUser);
        await context.SaveChangesAsync();

        var firstInstructor = new Instructor
        {
            UserId = firstInstructorUser.Id,
            Firstname = "First",
            Lastname = "Instructor",
            CreatedAt = now,
            UpdatedAt = now
        };
        var secondInstructor = new Instructor
        {
            UserId = secondInstructorUser.Id,
            Firstname = "Second",
            Lastname = "Instructor",
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Instructors.AddRange(firstInstructor, secondInstructor);
        await context.SaveChangesAsync();

        var timeIn = new TimeOnly(7, 30);
        var timeOut = new TimeOnly(9, 30);
        context.Schedules.AddRange(
            new Schedules
            {
                SubjectId = subject.Id,
                ClassroomId = firstClassroom.Id,
                SectionId = firstSection.Id,
                InstructorId = firstInstructor.Id,
                DayOfWeek = "Monday",
                TimeIn = timeIn,
                TimeOut = timeOut,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Schedules
            {
                SubjectId = subject.Id,
                ClassroomId = secondClassroom.Id,
                SectionId = secondSection.Id,
                InstructorId = secondInstructor.Id,
                DayOfWeek = "Monday",
                TimeIn = timeIn,
                TimeOut = timeOut,
                CreatedAt = now,
                UpdatedAt = now
            });

        await context.SaveChangesAsync();

        var matchingScheduleCount = await context.Schedules
            .CountAsync(schedule => schedule.DayOfWeek == "Monday" && schedule.TimeIn == timeIn && schedule.TimeOut == timeOut);

        Assert.Equal(2, matchingScheduleCount);
    }

    private sealed class CountingSeederService : IDataSeederService
    {
        public int CallCount { get; private set; }

        public Task SeedDataAsync()
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
