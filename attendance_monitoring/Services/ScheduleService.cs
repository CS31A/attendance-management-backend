using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services
{
    public class ScheduleService(
        IScheduleRepository scheduleRepository,
        IInstructorRepository instructorRepository,
        IUserContextService userContextService,
        IHttpContextAccessor httpContextAccessor,
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
                var allSchedules = schedules.Select(ScheduleServiceSupport.MapToResponseDto).ToList();
                logger.LogInformation("Successfully retrieved {Count} schedules", allSchedules.Count);
                return allSchedules;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving all schedules");
                throw new EntityServiceException("Schedule", "GetAllSchedules", "An error occurred while retrieving schedules", ex);
            }
        }

        public async Task<ScheduleResponseDto> GetScheduleByIdAsync(int id)
        {
            logger.LogInformation("Retrieving schedule by ID: {Id}", id);
            try
            {
                var schedule = await scheduleRepository.GetScheduleByIdAsync(id).ConfigureAwait(false);
                if (schedule == null)
                {
                    logger.LogWarning("Schedule with ID {Id} not found", id);
                    throw new EntityNotFoundException<int>("Schedule", id);
                }

                logger.LogInformation("Successfully retrieved schedule with ID: {Id}", id);
                return ScheduleServiceSupport.MapToResponseDto(schedule);
            }
            catch (EntityNotFoundException<int>)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving schedule with ID {Id}", id);
                throw new EntityServiceException("Schedule", $"GetScheduleById: {id}", "An error occurred while retrieving the schedule", ex);
            }
        }

        public async Task<ScheduleResponseDto> GetScheduleByUuidAsync(Guid uuid)
        {
            logger.LogInformation("Retrieving schedule by UUID: {Uuid}", uuid);
            try
            {
                var schedule = await scheduleRepository.GetScheduleByUuidAsync(uuid).ConfigureAwait(false);
                if (schedule == null)
                {
                    logger.LogWarning("Schedule with UUID {Uuid} not found", uuid);
                    throw new EntityNotFoundException<Guid>("Schedule", uuid);
                }

                logger.LogInformation("Successfully retrieved schedule with UUID: {Uuid}", uuid);
                return ScheduleServiceSupport.MapToResponseDto(schedule);
            }
            catch (EntityNotFoundException<Guid>)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving schedule with UUID {Uuid}", uuid);
                throw new EntityServiceException("Schedule", $"GetScheduleByUuid: {uuid}", "An error occurred while retrieving the schedule", ex);
            }
        }

        public async Task<IEnumerable<ScheduleResponseDto>> GetSchedulesByInstructorIdAsync(int instructorId)
        {
            logger.LogInformation("Retrieving schedules for instructor ID: {InstructorId}", instructorId);
            try
            {
                var schedules = await scheduleRepository.GetSchedulesByInstructorIdAsync(instructorId);
                var instructorSchedules = schedules.Select(ScheduleServiceSupport.MapToResponseDto).ToList();
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

        public async Task<IEnumerable<ScheduleResponseDto>> GetSchedulesBySectionIdAsync(int sectionId)
        {
            logger.LogInformation("Retrieving schedules for section ID: {SectionId}", sectionId);
            try
            {
                var schedules = await scheduleRepository.GetSchedulesBySectionIdAsync(sectionId);
                var sectionSchedules = schedules.Select(ScheduleServiceSupport.MapToResponseDto).ToList();
                logger.LogInformation("Successfully retrieved {Count} schedules for section ID: {SectionId}",
                    sectionSchedules.Count, sectionId);
                return sectionSchedules;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving schedules for section ID {SectionId}", sectionId);
                throw new EntityServiceException("Schedule", $"GetSchedulesBySectionId: {sectionId}",
                    "An error occurred while retrieving schedules for the section", ex);
            }
        }

        public async Task<IEnumerable<ScheduleResponseDto>> GetMySchedulesAsync()
        {
            logger.LogInformation("Retrieving schedules for the current instructor");
            try
            {
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext?.User == null)
                {
                    var errorMessage = "User context not found.";
                    logger.LogWarning("GetMySchedules failed: {ErrorMessage}", errorMessage);
                    throw new EntityUnauthorizedException("Schedule", "GetMySchedules", "unknown", errorMessage);
                }

                var userId = await userContextService.GetUserIdAsync(httpContext.User).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    var errorMessage = "User ID not found in context.";
                    logger.LogWarning("GetMySchedules failed: {ErrorMessage}", errorMessage);
                    throw new EntityUnauthorizedException("Schedule", "GetMySchedules", "unknown", errorMessage);
                }

                // Get instructor by user ID
                var instructor = await instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
                if (instructor == null)
                {
                    var errorMessage = "Instructor profile not found for the current user.";
                    logger.LogWarning("GetMySchedules failed: {ErrorMessage}", errorMessage);
                    throw new EntityUnauthorizedException("Schedule", "GetMySchedules", userId, errorMessage);
                }
                return await GetSchedulesByInstructorIdAsync(instructor.Id).ConfigureAwait(false);
            }
            catch (EntityUnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving schedules for the current instructor");
                throw new EntityServiceException("Schedule", "GetMySchedules",
                    "An error occurred while retrieving your schedules", ex);
            }
        }

        #endregion

        #region Dependency Check Operations
        public async Task<bool> HasSessionsInScheduleAsync(int id)
        {
            logger.LogInformation("Checking if schedule {ScheduleId} has sessions", id);
            try
            {
                var hasSessions = await scheduleRepository.HasSessionsInScheduleAsync(id).ConfigureAwait(false);
                logger.LogInformation("Schedule {ScheduleId} has sessions: {HasSessions}", id, hasSessions);
                return hasSessions;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking if schedule {ScheduleId} has sessions", id);
                throw new EntityServiceException("Schedule", $"HasSessionsInSchedule: {id}", "Error checking schedule dependencies", ex);
            }
        }
        #endregion
        #region Create Operations
        public async Task<Schedules> CreateScheduleAsync(CreateSchedule createSchedule)
        {
            logger.LogInformation("Creating new schedule with TimeIn: {TimeIn} and TimeOut: {TimeOut}",
                createSchedule.TimeIn, createSchedule.TimeOut);
            try
            {
                // Validate DayOfWeek
                if (!Constants.ScheduleConstants.IsValidDayOfWeek(createSchedule.DayOfWeek))
                {
                    logger.LogWarning("Schedule creation failed: Invalid DayOfWeek '{DayOfWeek}'", createSchedule.DayOfWeek);
                    throw new ValidationException($"Invalid DayOfWeek. Must be one of: {string.Join(", ", Constants.ScheduleConstants.ValidDaysOfWeek)}");
                }

                // Validate time range (TimeOut must be after TimeIn)
                if (createSchedule.TimeOut <= createSchedule.TimeIn)
                {
                    logger.LogWarning("Schedule creation failed: TimeOut ({TimeOut}) must be after TimeIn ({TimeIn})",
                        createSchedule.TimeOut, createSchedule.TimeIn);
                    throw new ValidationException("TimeOut must be after TimeIn");
                }

                // Validate relationships if needed
                var subjectId = await ScheduleServiceSupport.ResolveSubjectIdAsync(context, createSchedule.SubjectId, createSchedule.SubjectUuid).ConfigureAwait(false);
                var classroomId = await ScheduleServiceSupport.ResolveClassroomIdAsync(context, createSchedule.ClassroomId, createSchedule.ClassroomUuid).ConfigureAwait(false);
                var sectionId = await ScheduleServiceSupport.ResolveSectionIdAsync(context, createSchedule.SectionId, createSchedule.SectionUuid).ConfigureAwait(false);
                var instructorId = await ScheduleServiceSupport.ResolveInstructorIdAsync(context, createSchedule.InstructorId, createSchedule.InstructorUuid).ConfigureAwait(false);

                var schedule = new Schedules
                {
                    TimeIn = createSchedule.TimeIn,
                    TimeOut = createSchedule.TimeOut,
                    DayOfWeek = createSchedule.DayOfWeek,
                    SubjectId = subjectId,
                    ClassroomId = classroomId,
                    SectionId = sectionId,
                    InstructorId = instructorId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdSchedule = await scheduleRepository.AddScheduleAsync(schedule);

                logger.LogInformation("Successfully created schedule with ID: {Id}", createdSchedule.Id);
                return createdSchedule;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (EntityNotFoundException<int>)
            {
                throw;
            }
            catch (EntityNotFoundException<Guid>)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while creating schedule");
                throw ExceptionHandlingHelper.CreateServiceException("Schedule", "CreateSchedule", ex);
            }
        }

        #endregion
        #region Update Operations
        public async Task<Schedules> UpdateScheduleAsync(int id, UpdateSchedule updateSchedule)
        {
            logger.LogInformation("Updating schedule with ID: {Id}", id);
            try
            {
                var existingSchedule = await scheduleRepository.GetScheduleByIdAsync(id);
                if (existingSchedule == null)
                {
                    logger.LogWarning("Schedule update failed: Schedule with ID {Id} not found", id);
                    throw new EntityNotFoundException<int>("Schedule", id);
                }

                // Validate DayOfWeek if provided
                if (!string.IsNullOrEmpty(updateSchedule.DayOfWeek) &&
                    !Constants.ScheduleConstants.IsValidDayOfWeek(updateSchedule.DayOfWeek))
                {
                    logger.LogWarning("Schedule update failed: Invalid DayOfWeek '{DayOfWeek}'", updateSchedule.DayOfWeek);
                    throw new ValidationException($"Invalid DayOfWeek. Must be one of: {string.Join(", ", Constants.ScheduleConstants.ValidDaysOfWeek)}");
                }

                // Determine effective TimeIn and TimeOut for validation
                var effectiveTimeIn = updateSchedule.TimeIn ?? existingSchedule.TimeIn;
                var effectiveTimeOut = updateSchedule.TimeOut ?? existingSchedule.TimeOut;

                // Validate time range (TimeOut must be after TimeIn)
                if (effectiveTimeOut <= effectiveTimeIn)
                {
                    logger.LogWarning("Schedule update failed: TimeOut ({TimeOut}) must be after TimeIn ({TimeIn})",
                        effectiveTimeOut, effectiveTimeIn);
                    throw new ValidationException("TimeOut must be after TimeIn");
                }

                // Validate and update relationships only if provided
                if (updateSchedule.SubjectId.HasValue || (updateSchedule.SubjectUuid.HasValue && updateSchedule.SubjectUuid.Value != Guid.Empty))
                {
                    existingSchedule.SubjectId = await ScheduleServiceSupport.ResolveSubjectIdAsync(context, updateSchedule.SubjectId, updateSchedule.SubjectUuid).ConfigureAwait(false);
                }

                if (updateSchedule.ClassroomId.HasValue || (updateSchedule.ClassroomUuid.HasValue && updateSchedule.ClassroomUuid.Value != Guid.Empty))
                {
                    existingSchedule.ClassroomId = await ScheduleServiceSupport.ResolveClassroomIdAsync(context, updateSchedule.ClassroomId, updateSchedule.ClassroomUuid).ConfigureAwait(false);
                }

                if (updateSchedule.SectionId.HasValue || (updateSchedule.SectionUuid.HasValue && updateSchedule.SectionUuid.Value != Guid.Empty))
                {
                    existingSchedule.SectionId = await ScheduleServiceSupport.ResolveSectionIdAsync(context, updateSchedule.SectionId, updateSchedule.SectionUuid).ConfigureAwait(false);
                }

                if (updateSchedule.InstructorId.HasValue || (updateSchedule.InstructorUuid.HasValue && updateSchedule.InstructorUuid.Value != Guid.Empty))
                {
                    existingSchedule.InstructorId = await ScheduleServiceSupport.ResolveInstructorIdAsync(context, updateSchedule.InstructorId, updateSchedule.InstructorUuid).ConfigureAwait(false);
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
                    throw new EntityServiceException("Schedule", $"UpdateSchedule: {id}", "Failed to update schedule");
                }

                logger.LogInformation("Successfully updated schedule with ID: {Id}", updatedSchedule.Id);
                return updatedSchedule;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (EntityNotFoundException<int>)
            {
                throw;
            }
            catch (EntityNotFoundException<Guid>)
            {
                throw;
            }
            catch (EntityServiceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating schedule with ID: {Id}", id);
                throw ExceptionHandlingHelper.CreateServiceException("Schedule", $"UpdateSchedule: {id}", ex);
            }
        }

        #endregion

        public async Task<Schedules> UpdateScheduleByUuidAsync(Guid uuid, UpdateSchedule updateSchedule)
        {
            var existingSchedule = await scheduleRepository.GetScheduleByUuidAsync(uuid).ConfigureAwait(false);
            if (existingSchedule == null)
            {
                throw new EntityNotFoundException<Guid>("Schedule", uuid);
            }

            return await UpdateScheduleAsync(existingSchedule.Id, updateSchedule).ConfigureAwait(false);
        }

        #region Delete Operations

        public async Task DeleteScheduleAsync(int id, ClaimsPrincipal user)
        {
            logger.LogInformation("Deleting schedule with ID: {Id}", id);
            try
            {
                var existingSchedule = await scheduleRepository.GetScheduleByIdAsync(id).ConfigureAwait(false);
                if (existingSchedule == null)
                {
                    logger.LogWarning("Schedule deletion failed: Schedule with ID {Id} not found", id);
                    throw new EntityNotFoundException<int>("Schedule", id);
                }

                var result = await scheduleRepository.DeleteScheduleAsync(id).ConfigureAwait(false);
                if (!result)
                {
                    logger.LogWarning("Schedule deletion failed: Schedule with ID {Id} not found", id);
                    throw new EntityNotFoundException<int>("Schedule", id);
                }

                var rowsAffected = await scheduleRepository.SaveChangesAsync().ConfigureAwait(false);
                if (rowsAffected == 0)
                {
                    logger.LogWarning("Schedule deletion failed: Schedule with ID {Id} may have been deleted by another process", id);
                    throw new EntityServiceException("Schedule", $"DeleteSchedule: {id}", "Schedule may have been deleted by another process.");
                }

                logger.LogInformation("Successfully deleted schedule with ID: {Id}", id);
            }
            catch (EntityNotFoundException<int>)
            {
                throw;
            }
            catch (EntityServiceException)
            {
                throw;
            }
            catch (DbUpdateException ex) when (ExceptionHandlingHelper.IsForeignKeyViolation(ex))
            {
                var conflictType = ResolveDeleteConflictType(ex);
                var conflictMessage = ResolveDeleteConflictMessage(conflictType);
                logger.LogWarning(ex, "Schedule deletion failed due to foreign key constraint: {Message}", conflictMessage);
                throw new EntityConflictException("Schedule", conflictType, conflictMessage, ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while deleting schedule with ID: {Id}", id);
                throw ExceptionHandlingHelper.CreateServiceException("Schedule", $"DeleteSchedule: {id}", ex);
            }
        }

        public async Task DeleteScheduleByUuidAsync(Guid uuid, ClaimsPrincipal user)
        {
            var existingSchedule = await scheduleRepository.GetScheduleByUuidAsync(uuid).ConfigureAwait(false);
            if (existingSchedule == null)
            {
                throw new EntityNotFoundException<Guid>("Schedule", uuid);
            }

            await DeleteScheduleAsync(existingSchedule.Id, user).ConfigureAwait(false);
        }
        #endregion

        private static string ResolveDeleteConflictType(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;
            if (message.Contains("FK_Sessions_Schedules_ScheduleId", StringComparison.OrdinalIgnoreCase) || message.Contains("Sessions", StringComparison.OrdinalIgnoreCase))
            {
                return "sessions";
            }

            return "dependencies";
        }

        private static string ResolveDeleteConflictMessage(string conflictType)
        {
            return conflictType switch
            {
                "sessions" => "Cannot delete: Schedule has sessions assigned. Remove sessions first.",
                _ => "Cannot delete: Schedule has dependencies that prevent deletion.",
            };
        }

    }
}
