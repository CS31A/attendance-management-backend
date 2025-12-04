using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services
{
    public class ScheduleService(
        IScheduleRepository scheduleRepository,
        ApplicationDbContext context,
        ILogger<ScheduleService> logger)
        : IScheduleService
    {
        #region Get Operations
        public async Task<IEnumerable<ScheduleResponseDto>> GetAllSchedulesAsync()
        {
            logger.LogInformation("Retrieving all schedules");
            try
            {
                var schedules = await scheduleRepository.GetAllSchedulesAsync();
                var allSchedules = schedules.Select(MapToResponseDto).ToList();
                logger.LogInformation("Successfully retrieved {Count} schedules", allSchedules.Count);
                return allSchedules;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving all schedules");
                throw new EntityServiceException("Schedule", "GetAllSchedules", "An error occurred while retrieving schedules", ex);
            }
        }

        public async Task<ScheduleResponseDto?> GetScheduleByIdAsync(int id)
        {
            logger.LogInformation("Retrieving schedule by ID: {Id}", id);
            try
            {
                var schedule = await scheduleRepository.GetScheduleByIdAsync(id).ConfigureAwait(false);
                if (schedule == null)
                {
                    logger.LogWarning("Schedule with ID {Id} not found", id);
                    return null;
                }

                logger.LogInformation("Successfully retrieved schedule with ID: {Id}", id);
                return MapToResponseDto(schedule);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving schedule with ID {Id}", id);
                throw new EntityServiceException("Schedule", $"GetScheduleById: {id}", "An error occurred while retrieving the schedule", ex);
            }
        }

        public async Task<IEnumerable<ScheduleResponseDto>> GetSchedulesByInstructorIdAsync(int instructorId)
        {
            logger.LogInformation("Retrieving schedules for instructor ID: {InstructorId}", instructorId);
            try
            {
                var schedules = await scheduleRepository.GetSchedulesByInstructorIdAsync(instructorId);
                var instructorSchedules = schedules.Select(MapToResponseDto).ToList();
                logger.LogInformation("Successfully retrieved {Count} schedules for instructor ID: {InstructorId}",
                    instructorSchedules.Count, instructorId);
                return instructorSchedules;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving schedules for instructor ID {InstructorId}", instructorId);
                throw new EntityServiceException("Schedule", $"GetSchedulesByInstructorId: {instructorId}",
                    "An error occurred while retrieving schedules for the instructor", ex);
            }
        }

        #endregion

        #region Create Operations
        public async Task<(Schedules?, string?)> CreateScheduleAsync(CreateSchedule createSchedule)
        {
            logger.LogInformation("Creating new schedule with TimeIn: {TimeIn} and TimeOut: {TimeOut}",
                createSchedule.TimeIn, createSchedule.TimeOut);
            try
            {
                // Validate DayOfWeek
                if (!Constants.ScheduleConstants.IsValidDayOfWeek(createSchedule.DayOfWeek))
                {
                    logger.LogWarning("Schedule creation failed: Invalid DayOfWeek '{DayOfWeek}'", createSchedule.DayOfWeek);
                    return (null, $"Invalid DayOfWeek. Must be one of: {string.Join(", ", Constants.ScheduleConstants.ValidDaysOfWeek)}");
                }

                // Validate time range (TimeOut must be after TimeIn)
                if (createSchedule.TimeOut <= createSchedule.TimeIn)
                {
                    logger.LogWarning("Schedule creation failed: TimeOut ({TimeOut}) must be after TimeIn ({TimeIn})",
                        createSchedule.TimeOut, createSchedule.TimeIn);
                    return (null, "TimeOut must be after TimeIn");
                }

                // Validate relationships if needed
                var subjectExists = await context.Subjects.AsNoTracking().AnyAsync(s => s.Id == createSchedule.SubjectId);
                if (!subjectExists)
                {
                    logger.LogWarning("Schedule creation failed: Subject with ID {SubjectId} not found", createSchedule.SubjectId);
                    return (null, "Subject not found");
                }

                var classroomExists = await context.Classrooms.AsNoTracking().AnyAsync(c => c.Id == createSchedule.ClassroomId);
                if (!classroomExists)
                {
                    logger.LogWarning("Schedule creation failed: Classroom with ID {ClassroomId} not found", createSchedule.ClassroomId);
                    return (null, "Classroom not found");
                }

                var sectionExists = await context.Sections.AsNoTracking().AnyAsync(s => s.Id == createSchedule.SectionId);
                if (!sectionExists)
                {
                    logger.LogWarning("Schedule creation failed: Section with ID {SectionId} not found", createSchedule.SectionId);
                    return (null, "Section not found");
                }

                var instructorExists = await context.Instructors.AsNoTracking().AnyAsync(i => i.Id == createSchedule.InstructorId);
                if (!instructorExists)
                {
                    logger.LogWarning("Schedule creation failed: Instructor with ID {InstructorId} not found", createSchedule.InstructorId);
                    return (null, "Instructor not found");
                }

                var schedule = new Schedules
                {
                    TimeIn = createSchedule.TimeIn,
                    TimeOut = createSchedule.TimeOut,
                    DayOfWeek = createSchedule.DayOfWeek,
                    SubjectId = createSchedule.SubjectId,
                    ClassroomId = createSchedule.ClassroomId,
                    SectionId = createSchedule.SectionId,
                    InstructorId = createSchedule.InstructorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdSchedule = await scheduleRepository.AddScheduleAsync(schedule);

                logger.LogInformation("Successfully created schedule with ID: {Id}", createdSchedule.Id);
                return (createdSchedule, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while creating schedule");
                throw new EntityServiceException("Schedule", "CreateSchedule", "An error occurred while creating the schedule", ex);
            }
        }

        #endregion

        #region Update Operations
        public async Task<(Schedules?, string?)> UpdateScheduleAsync(int id, UpdateSchedule updateSchedule)
        {
            logger.LogInformation("Updating schedule with ID: {Id}", id);

            try
            {
                var existingSchedule = await scheduleRepository.GetScheduleByIdAsync(id);
                if (existingSchedule == null)
                {
                    logger.LogWarning("Schedule update failed: Schedule with ID {Id} not found", id);
                    return (null, "Schedule not found");
                }

                // Validate DayOfWeek if provided
                if (!string.IsNullOrEmpty(updateSchedule.DayOfWeek) &&
                    !Constants.ScheduleConstants.IsValidDayOfWeek(updateSchedule.DayOfWeek))
                {
                    logger.LogWarning("Schedule update failed: Invalid DayOfWeek '{DayOfWeek}'", updateSchedule.DayOfWeek);
                    return (null, $"Invalid DayOfWeek. Must be one of: {string.Join(", ", Constants.ScheduleConstants.ValidDaysOfWeek)}");
                }

                // Determine effective TimeIn and TimeOut for validation
                var effectiveTimeIn = updateSchedule.TimeIn ?? existingSchedule.TimeIn;
                var effectiveTimeOut = updateSchedule.TimeOut ?? existingSchedule.TimeOut;

                // Validate time range (TimeOut must be after TimeIn)
                if (effectiveTimeOut <= effectiveTimeIn)
                {
                    logger.LogWarning("Schedule update failed: TimeOut ({TimeOut}) must be after TimeIn ({TimeIn})",
                        effectiveTimeOut, effectiveTimeIn);
                    return (null, "TimeOut must be after TimeIn");
                }

                // Validate and update relationships only if provided
                if (updateSchedule.SubjectId.HasValue)
                {
                    var subjectExists = await context.Subjects.AsNoTracking().AnyAsync(s => s.Id == updateSchedule.SubjectId.Value);
                    if (!subjectExists)
                    {
                        logger.LogWarning("Schedule update failed: Subject with ID {SubjectId} not found", updateSchedule.SubjectId.Value);
                        return (null, "Subject not found");
                    }
                    existingSchedule.SubjectId = updateSchedule.SubjectId.Value;
                }

                if (updateSchedule.ClassroomId.HasValue)
                {
                    var classroomExists = await context.Classrooms.AsNoTracking().AnyAsync(c => c.Id == updateSchedule.ClassroomId.Value);
                    if (!classroomExists)
                    {
                        logger.LogWarning("Schedule update failed: Classroom with ID {ClassroomId} not found", updateSchedule.ClassroomId.Value);
                        return (null, "Classroom not found");
                    }
                    existingSchedule.ClassroomId = updateSchedule.ClassroomId.Value;
                }

                if (updateSchedule.SectionId.HasValue)
                {
                    var sectionExists = await context.Sections.AsNoTracking().AnyAsync(s => s.Id == updateSchedule.SectionId.Value);
                    if (!sectionExists)
                    {
                        logger.LogWarning("Schedule update failed: Section with ID {SectionId} not found", updateSchedule.SectionId.Value);
                        return (null, "Section not found");
                    }
                    existingSchedule.SectionId = updateSchedule.SectionId.Value;
                }

                if (updateSchedule.InstructorId.HasValue)
                {
                    var instructorExists = await context.Instructors.AsNoTracking().AnyAsync(i => i.Id == updateSchedule.InstructorId.Value);
                    if (!instructorExists)
                    {
                        logger.LogWarning("Schedule update failed: Instructor with ID {InstructorId} not found", updateSchedule.InstructorId.Value);
                        return (null, "Instructor not found");
                    }
                    existingSchedule.InstructorId = updateSchedule.InstructorId.Value;
                }

                // Update simple fields only if provided
                if (updateSchedule.TimeIn.HasValue)
                {
                    existingSchedule.TimeIn = updateSchedule.TimeIn.Value;
                }

                if (updateSchedule.TimeOut.HasValue)
                {
                    existingSchedule.TimeOut = updateSchedule.TimeOut.Value;
                }

                if (!string.IsNullOrEmpty(updateSchedule.DayOfWeek))
                {
                    existingSchedule.DayOfWeek = updateSchedule.DayOfWeek;
                }

                existingSchedule.UpdatedAt = DateTime.UtcNow;

                var updatedSchedule = await scheduleRepository.UpdateScheduleAsync(existingSchedule);

                if (updatedSchedule == null)
                {
                    logger.LogWarning("Schedule update failed: Failed to update schedule with ID {Id}", id);
                    return (null, "Failed to update schedule");
                }

                logger.LogInformation("Successfully updated schedule with ID: {Id}", updatedSchedule.Id);
                return (updatedSchedule, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating schedule with ID: {Id}", id);
                throw new EntityServiceException("Schedule", $"UpdateSchedule: {id}", "An error occurred while updating the schedule", ex);
            }
        }

        #endregion

        #region Delete Operations

        public async Task<string?> DeleteScheduleAsync(int id, ClaimsPrincipal user)
        {
            logger.LogInformation("Deleting schedule with ID: {Id}", id);

            try
            {
                var existingSchedule = await scheduleRepository.GetScheduleByIdAsync(id).ConfigureAwait(false);
                if (existingSchedule == null)
                {
                    logger.LogWarning("Schedule deletion failed: Schedule with ID {Id} not found", id);
                    return "Schedule not found";
                }

                var result = await scheduleRepository.DeleteScheduleAsync(id).ConfigureAwait(false);
                if (!result)
                {
                    logger.LogWarning("Schedule deletion failed: Schedule with ID {Id} not found", id);
                    return "Schedule not found";
                }

                var rowsAffected = await scheduleRepository.SaveChangesAsync().ConfigureAwait(false);
                if (rowsAffected == 0)
                {
                    logger.LogWarning("Schedule deletion failed: Schedule with ID {Id} may have been deleted by another process", id);
                    return "Schedule may have been deleted by another process.";
                }

                logger.LogInformation("Successfully deleted schedule with ID: {Id}", id);
                return null;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true
                                               || ex.InnerException?.Message.Contains("FK_") == true)
            {
                // Handle foreign key constraint violations with user-friendly messages
                var innerMessage = ex.InnerException?.Message ?? ex.Message;

                string userFriendlyMessage;
                if (innerMessage.Contains("FK_QrCodes_Schedules") || innerMessage.Contains("QrCodes"))
                {
                    userFriendlyMessage = "Cannot delete schedule because it has associated QR codes. Please delete or revoke the QR codes first.";
                }
                else if (innerMessage.Contains("FK_Attendance") || innerMessage.Contains("Attendance"))
                {
                    userFriendlyMessage = "Cannot delete schedule because it has associated attendance records. Schedules with attendance history cannot be deleted.";
                }
                else
                {
                    userFriendlyMessage = "Cannot delete schedule because it has associated records. Please remove all dependencies first.";
                }

                logger.LogWarning(ex, "Schedule deletion failed due to foreign key constraint: {Message}", userFriendlyMessage);
                throw new EntityServiceException("Schedule", $"DeleteSchedule: {id}", userFriendlyMessage, ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while deleting schedule with ID: {Id}", id);
                throw new EntityServiceException("Schedule", $"DeleteSchedule: {id}", "An error occurred while deleting the schedule", ex);
            }
        }

        #endregion

        #region Helper Methods

        public static ScheduleResponseDto MapToResponseDto(Schedules schedule)
        {
            return new ScheduleResponseDto
            {
                Id = schedule.Id,
                TimeIn = schedule.TimeIn,
                TimeOut = schedule.TimeOut,
                DayOfWeek = schedule.DayOfWeek,
                Subject = new SubjectResponseDto
                {
                    Id = schedule.Subject.Id,
                    Name = schedule.Subject.Name,
                    Code = schedule.Subject.Code,
                    CreatedAt = schedule.Subject.CreatedAt,
                    UpdatedAt = schedule.Subject.UpdatedAt
                },
                Classroom = new ClassroomResponseDto
                {
                    Id = schedule.Classroom.Id,
                    Name = schedule.Classroom.Name,
                    CreatedAt = schedule.Classroom.CreatedAt,
                    UpdatedAt = schedule.Classroom.UpdatedAt
                },
                Section = new SectionResponseDto
                {
                    Id = schedule.Section.Id,
                    Name = schedule.Section.Name,
                    CourseId = schedule.Section.CourseId,
                    CreatedAt = schedule.Section.CreatedAt,
                    UpdatedAt = schedule.Section.UpdatedAt
                },
                Instructor = new InstructorResponseDto
                {
                    Id = schedule.Instructor.Id,
                    Firstname = schedule.Instructor.Firstname,
                    Lastname = schedule.Instructor.Lastname,
                    Email = schedule.Instructor.User?.Email
                },
                CreatedAt = schedule.CreatedAt,
                UpdatedAt = schedule.UpdatedAt
            };
        }

        #endregion
    }
}
