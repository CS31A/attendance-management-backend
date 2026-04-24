using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace attendance.testproject.Repositories_Testing;

public sealed class SliceAUuidRepositoryTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Course _course;
    private readonly Subject _subject;
    private readonly Section _section;
    private readonly Classroom _classroom;
    private readonly Instructor _instructor;
    private readonly Schedules _schedule;
    private readonly Student _student;
    private readonly StudentEnrollment _enrollment;
    private readonly IdentityUser _instructorUser;
    private readonly IdentityUser _studentUser;

    public SliceAUuidRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        _course = new Course
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Name = "Computer Science",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _subject = new Subject
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Name = "Mathematics",
            Code = "MATH101",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _classroom = new Classroom
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Name = "Room 101",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _section = new Section
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Name = "CS-3A",
            CourseId = _course.Id,
            Course = _course,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _instructor = new Instructor
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Firstname = "Ada",
            Lastname = "Lovelace",
            UserId = "instructor-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _instructorUser = new IdentityUser
        {
            Id = _instructor.UserId,
            UserName = "ada@example.com",
            NormalizedUserName = "ADA@EXAMPLE.COM",
            Email = "ada@example.com",
            NormalizedEmail = "ADA@EXAMPLE.COM",
        };

        _student = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Firstname = "Grace",
            Lastname = "Hopper",
            UserId = "student-1",
            SectionId = _section.Id,
            Section = _section,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _studentUser = new IdentityUser
        {
            Id = _student.UserId,
            UserName = "grace@example.com",
            NormalizedUserName = "GRACE@EXAMPLE.COM",
            Email = "grace@example.com",
            NormalizedEmail = "GRACE@EXAMPLE.COM",
        };

        _schedule = new Schedules
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Monday",
            SubjectId = _subject.Id,
            Subject = _subject,
            ClassroomId = _classroom.Id,
            Classroom = _classroom,
            SectionId = _section.Id,
            Section = _section,
            InstructorId = _instructor.Id,
            Instructor = _instructor,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _enrollment = new StudentEnrollment
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            StudentId = _student.Id,
            Student = _student,
            SectionId = _section.Id,
            Section = _section,
            SubjectId = _subject.Id,
            Subject = _subject,
            EnrollmentType = "Irregular",
            IsActive = true,
            EnrolledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.Users.Add(_instructorUser);
        _context.Users.Add(_studentUser);
        _context.Courses.Add(_course);
        _context.Subjects.Add(_subject);
        _context.Classrooms.Add(_classroom);
        _context.Sections.Add(_section);
        _context.Instructors.Add(_instructor);
        _context.Students.Add(_student);
        _context.Schedules.Add(_schedule);
        _context.StudentEnrollments.Add(_enrollment);
        _context.SaveChanges();

        _schedule.Uuid = _context.Schedules.AsNoTracking().Single(s => s.Id == _schedule.Id).Uuid;
        _enrollment.Uuid = _context.StudentEnrollments.AsNoTracking().Single(se => se.Id == _enrollment.Id).Uuid;
        _context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task CourseRepository_GetCourseByUuidAsync_ReturnsReadOnlyAndTrackedCourse()
    {
        var repository = new CourseRepository(_context);

        var readOnlyCourse = await repository.GetCourseByUuidAsync(_course.Uuid);
        var trackedCourse = await repository.GetCourseByUuidTrackedAsync(_course.Uuid);

        Assert.NotNull(readOnlyCourse);
        Assert.Equal(_course.Id, readOnlyCourse.Id);
        Assert.NotNull(trackedCourse);
        Assert.Equal(EntityState.Unchanged, _context.Entry(trackedCourse).State);
    }

    [Fact]
    public async Task SubjectRepository_GetSubjectByUuidAsync_ReturnsReadOnlyAndTrackedSubject()
    {
        var repository = new SubjectRepository(_context);

        var readOnlySubject = await repository.GetSubjectByUuidAsync(_subject.Uuid);
        var trackedSubject = await repository.GetSubjectByUuidTrackedAsync(_subject.Uuid);

        Assert.NotNull(readOnlySubject);
        Assert.Equal(_subject.Code, readOnlySubject.Code);
        Assert.NotNull(trackedSubject);
        Assert.Equal(EntityState.Unchanged, _context.Entry(trackedSubject).State);
    }

    [Fact]
    public async Task SectionRepository_GetSectionByUuidAsync_ReturnsReadOnlyAndTrackedSection()
    {
        var repository = new SectionRepository(_context, NullLogger<SectionRepository>.Instance);

        var readOnlySection = await repository.GetSectionByUuidAsync(_section.Uuid);
        var trackedSection = await repository.GetSectionByUuidTrackedAsync(_section.Uuid);

        Assert.NotNull(readOnlySection);
        Assert.Equal(_section.CourseId, readOnlySection.CourseId);
        Assert.NotNull(trackedSection);
        Assert.Equal(EntityState.Unchanged, _context.Entry(trackedSection).State);
    }

    [Fact]
    public async Task ClassroomRepository_GetClassroomByUuidAsync_ReturnsReadOnlyAndTrackedClassroom()
    {
        var repository = new ClassroomRepository(_context);

        var readOnlyClassroom = await repository.GetClassroomByUuidAsync(_classroom.Uuid);
        var trackedClassroom = await repository.GetClassroomByUuidTrackedAsync(_classroom.Uuid);

        Assert.NotNull(readOnlyClassroom);
        Assert.Equal(_classroom.Name, readOnlyClassroom.Name);
        Assert.NotNull(trackedClassroom);
        Assert.Equal(EntityState.Unchanged, _context.Entry(trackedClassroom).State);
    }

    [Fact]
    public async Task ScheduleRepository_GetScheduleByUuidAsync_ReturnsReadOnlyAndTrackedScheduleWithNavigations()
    {
        var repository = new ScheduleRepository(_context);
        var scheduleUuid = _context.Schedules.AsNoTracking().Where(s => s.Id == _schedule.Id).Select(s => s.Uuid).Single();

        var readOnlySchedule = await repository.GetScheduleByUuidAsync(scheduleUuid);
        var trackedSchedule = await repository.GetScheduleByUuidTrackedAsync(scheduleUuid);

        Assert.NotNull(readOnlySchedule);
        Assert.Equal(_schedule.Id, readOnlySchedule.Id);
        Assert.Equal(_subject.Id, readOnlySchedule.Subject.Id);
        Assert.Equal(_classroom.Id, readOnlySchedule.Classroom.Id);
        Assert.NotNull(trackedSchedule);
        Assert.Equal(EntityState.Unchanged, _context.Entry(trackedSchedule).State);
    }

    [Fact]
    public async Task StudentEnrollmentRepository_GetByUuidAsync_ReturnsReadOnlyAndTrackedEnrollmentWithNavigations()
    {
        var repository = new StudentEnrollmentRepository(_context);
        var enrollmentUuid = _context.StudentEnrollments.AsNoTracking().Where(se => se.Id == _enrollment.Id).Select(se => se.Uuid).Single();

        var readOnlyEnrollment = await repository.GetByUuidAsync(enrollmentUuid);
        var trackedEnrollment = await repository.GetByUuidTrackedAsync(enrollmentUuid);

        Assert.NotNull(readOnlyEnrollment);
        Assert.Equal(_student.Id, readOnlyEnrollment.Student.Id);
        Assert.Equal(_section.Id, readOnlyEnrollment.Section.Id);
        Assert.Equal(_subject.Id, readOnlyEnrollment.Subject.Id);
        Assert.NotNull(trackedEnrollment);
        Assert.Equal(EntityState.Unchanged, _context.Entry(trackedEnrollment).State);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
