using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class InstructorRepository(ApplicationDbContext context) : IInstructorRepository
{
    #region Read Operations

    #region GetAllInstructorsAsync
    /// <summary>
    /// Retrieves all non-deleted instructors.
    /// Performance: Single query, no navigation properties loaded.
    /// Note: User navigation property has [JsonIgnore] and is not needed for API responses.
    /// </summary>
    public async Task<IEnumerable<Instructor>> GetAllInstructorsAsync()
    {
        return await context.Instructors
            .AsNoTracking()
            .Where(i => !i.IsDeleted)
            .OrderBy(i => i.Id)
            .ToListAsync().ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByIdAsync
    /// <summary>
    /// Retrieves an instructor by ID (read-only).
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByIdAsync(Guid id)
    {
        return await context.Instructors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByUuidAsync
    /// <summary>
    /// Retrieves an instructor by UUID (read-only).
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByUuidAsync(Guid id)
    {
        return await context.Instructors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByIdTrackedAsync
    /// <summary>
    /// Retrieves an instructor by ID with change tracking for updates.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByIdTrackedAsync(Guid id)
    {
        return await context.Instructors
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByUuidTrackedAsync
    /// <summary>
    /// Retrieves an instructor by UUID with change tracking for updates.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByUuidTrackedAsync(Guid id)
    {
        return await context.Instructors
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByUserIdAsync
    /// <summary>
    /// Retrieves an instructor by their Identity User ID.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByUserIdAsync(string userId)
    {
        return await context.Instructors
            .AsNoTracking()
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.UserId == userId && !i.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetInstructorByIdIgnoreDeleteStatus
    /// <summary>
    /// Retrieves an instructor by ID regardless of delete status.
    /// Performance: Single query, no navigation properties loaded.
    /// </summary>
    public async Task<Instructor?> GetInstructorByIdIgnoreDeleteStatus(Guid id)
    {
        return await context.Instructors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id)
            .ConfigureAwait(false);
    }
    #endregion

    #endregion

    #region Write Operations

    #region Create Operations

    #region CreateInstructorAsync
    public async Task<Instructor> CreateInstructorAsync(Instructor instructor)
    {
        var entry = await context.Instructors.AddAsync(instructor).ConfigureAwait(false);
        return entry.Entity;
    }
    #endregion

    #endregion

    #region Update Operations

    #region UpdateInstructorAsync
    public Task<Instructor> UpdateInstructorAsync(Instructor instructor)
    {
        instructor.UpdatedAt = DateTime.UtcNow;
        var entry = context.Instructors.Update(instructor);
        return Task.FromResult(entry.Entity);
    }
    #endregion

    #endregion

    #region Delete Operations

    #region SoftDeleteInstructorAsync
    public async Task<bool> SoftDeleteInstructorAsync(Guid id)
    {
        var instructor = await context.Instructors.FindAsync(id).ConfigureAwait(false);
        if (instructor == null)
            return false;

        instructor.IsDeleted = true;
        instructor.DeletedAt = DateTime.UtcNow;
        instructor.UpdatedAt = DateTime.UtcNow;

        context.Instructors.Update(instructor);
        return true;
    }
    #endregion

    #region HardDeleteInstructorAsync
    public async Task<bool> HardDeleteInstructorAsync(Guid id)
    {
        var instructor = await context.Instructors.FindAsync(id).ConfigureAwait(false);
        if (instructor == null)
            return false;

        context.Instructors.Remove(instructor);
        return true;
    }
    #endregion

    #region RestoreInstructorAsync
    public async Task<bool> RestoreInstructorAsync(Guid id)
    {
        var instructor = await context.Instructors.FindAsync(id).ConfigureAwait(false);
        if (instructor == null)
            return false;

        instructor.IsDeleted = false;
        instructor.DeletedAt = null;
        instructor.UpdatedAt = DateTime.UtcNow;

        context.Instructors.Update(instructor);
        return true;
    }
    #endregion

    #endregion

    #endregion

    #region Specialized Query Operations

    #region GetSchedulesWithRelatedDataByInstructorIdAsync
    /// <summary>
    /// Retrieves all schedules with related data for a specific instructor.
    /// Uses eager loading to minimize database round trips (N+1 query prevention).
    /// Includes: Schedule → Section → Course, Subject, Classroom, StudentEnrollments (with Students)
    /// Note: Regular students (Student.SectionId matches) must be queried separately in the service layer.
    /// Note: Soft-delete filtering is NOT applied to Schedules in this query.
    /// Performance: Single query with multiple joins.
    /// </summary>
    public async Task<IEnumerable<Schedules>> GetSchedulesWithRelatedDataByInstructorIdAsync(Guid instructorId)
    {
        return await context.Schedules
            .AsNoTracking()
            .AsSplitQuery()
            .Where(s => s.InstructorId == instructorId)
            .Include(s => s.Section)
                .ThenInclude(sec => sec.Course)
            .Include(s => s.Subject)
            .Include(s => s.Classroom)
            .Include(s => s.Section.StudentEnrollments.Where(se => se.IsActive))
                .ThenInclude(se => se.Student)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region GetRegularStudentsBySectionIdAsync
    /// <summary>
    /// Retrieves regular students whose primary section matches the supplied section.
    /// Soft-deleted students are excluded.
    /// </summary>
    public async Task<IEnumerable<Student>> GetRegularStudentsBySectionIdAsync(Guid sectionId)
    {
        return await context.Students
            .AsNoTracking()
            .Where(student => student.SectionId == sectionId && !student.IsDeleted)
            .OrderBy(student => student.Lastname)
            .ThenBy(student => student.Firstname)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region GetHandledSectionsByInstructorIdAsync
    /// <summary>
    /// Retrieves all sections handled by the instructor, including course data.
    /// Performance: Single query with course eager loading.
    /// </summary>
    public async Task<IEnumerable<Section>> GetHandledSectionsByInstructorIdAsync(Guid instructorId)
    {
        return await context.Sections
            .AsNoTracking()
            .Include(section => section.Course)
            .Where(section => context.Schedules.Any(schedule => schedule.InstructorId == instructorId && schedule.SectionId == section.Id))
            .OrderBy(section => section.Name)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region GetHandledClassesBySectionAndInstructorAsync
    /// <summary>
    /// Retrieves handled classes for a specific section and instructor with related data.
    /// Uses eager loading to prevent N+1 queries for subject, classroom, section, and enrolled students.
    /// </summary>
    public async Task<IEnumerable<Schedules>> GetHandledClassesBySectionAndInstructorAsync(Guid sectionId, Guid instructorId)
    {
        return await context.Schedules
            .AsNoTracking()
            .AsSplitQuery()
            .Where(schedule => schedule.SectionId == sectionId && schedule.InstructorId == instructorId)
            .Include(schedule => schedule.Subject)
            .Include(schedule => schedule.Classroom)
            .Include(schedule => schedule.Section)
                .ThenInclude(section => section.Course)
            .Include(schedule => schedule.Section)
                .ThenInclude(section => section.StudentEnrollments.Where(studentEnrollment => studentEnrollment.IsActive))
                    .ThenInclude(studentEnrollment => studentEnrollment.Student)
            .OrderBy(schedule => schedule.DayOfWeek)
            .ThenBy(schedule => schedule.TimeIn)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region GetHomeSectionStudentsAsync
    /// <summary>
    /// Retrieves all non-deleted students whose home section matches the supplied section.
    /// </summary>
    public async Task<IEnumerable<Student>> GetHomeSectionStudentsAsync(Guid sectionId)
    {
        return await GetRegularStudentsBySectionIdAsync(sectionId).ConfigureAwait(false);
    }
    #endregion

    #region IsInstructorHandlingSectionAsync
    /// <summary>
    /// Determines whether the instructor handles the supplied section.
    /// Performance: Uses an existence check without loading entities.
    /// </summary>
    public async Task<bool> IsInstructorHandlingSectionAsync(Guid instructorId, Guid sectionId)
    {
        return await context.Schedules
            .AsNoTracking()
            .AnyAsync(schedule => schedule.InstructorId == instructorId && schedule.SectionId == sectionId)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentWithDetailsAsync
    /// <summary>
    /// Retrieves a student with related section, course, and active enrollment data.
    /// Uses split query execution to avoid cartesian explosion when loading multiple navigations.
    /// </summary>
    public async Task<Student?> GetStudentWithDetailsAsync(Guid studentId)
    {
        return await context.Students
            .AsNoTracking()
            .AsSplitQuery()
            .Include(student => student.Section)
                .ThenInclude(section => section.Course)
            .Include(student => student.AdditionalEnrollments.Where(studentEnrollment => studentEnrollment.IsActive))
                .ThenInclude(studentEnrollment => studentEnrollment.Section)
            .Include(student => student.AdditionalEnrollments.Where(studentEnrollment => studentEnrollment.IsActive))
                .ThenInclude(studentEnrollment => studentEnrollment.Subject)
            .FirstOrDefaultAsync(student => student.Id == studentId && !student.IsDeleted)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetStudentAttendanceForInstructorSubjectsAsync
    /// <summary>
    /// Retrieves attendance records for a student in sessions taught by the supplied instructor.
    /// Uses eager loading for session and schedule data commonly needed by higher layers.
    /// </summary>
    public async Task<IEnumerable<AttendanceRecord>> GetStudentAttendanceForInstructorSubjectsAsync(Guid studentId, Guid instructorId)
    {
        return await context.AttendanceRecords
            .AsNoTracking()
            .AsSplitQuery()
            .Where(attendanceRecord => attendanceRecord.StudentId == studentId && attendanceRecord.Session.Schedule.InstructorId == instructorId)
            .Include(attendanceRecord => attendanceRecord.Session)
                .ThenInclude(session => session.Schedule)
                    .ThenInclude(schedule => schedule.Subject)
            .OrderByDescending(attendanceRecord => attendanceRecord.CheckInTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #endregion

    #region Utility Operations

    #region SaveChangesAsync
    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }
    #endregion

    #endregion
}
