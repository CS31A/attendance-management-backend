using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.Services
{
    public class ScheduleService(
        IScheduleRepository scheduleRepository,
        ApplicationDbContext context,
        ILogger<ScheduleService> logger)
        : IScheduleService
    {
        #region Get Operations
        public async Task<IEnumerable<Schedules>> GetAllSchedulesAsync()
        {
            logger.LogInformation("Retrieving all schedules");
            try
            {
                var schedules = await scheduleRepository.GetAllSchedulesAsync();
                var allSchedules = schedules.ToList();
                logger.LogInformation("Successfully retrieved {Count} schedules", allSchedules.Count);
                return allSchedules;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving all schedules");
                throw new ScheduleServiceException("GetAllSchedules", "An error occurred while retrieving schedules", ex);
            }
        }

        public async Task<Schedules?> GetScheduleByIdAsync(int id)
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
                return schedule;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving schedule with ID {Id}", id);
                throw new ScheduleServiceException($"GetScheduleById: {id}", "An error occurred while retrieving the schedule", ex);
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
                // Validate relationships if needed
                var subjectExists = await context.Subjects.AnyAsync(s => s.Id == createSchedule.SubjectId);
                if (!subjectExists)
                {
                    logger.LogWarning("Schedule creation failed: Subject with ID {SubjectId} not found", createSchedule.SubjectId);
                    return (null, "Subject not found");
                }

                var classroomExists = await context.Classrooms.AnyAsync(c => c.Id == createSchedule.ClassroomId);
                if (!classroomExists)
                {
                    logger.LogWarning("Schedule creation failed: Classroom with ID {ClassroomId} not found", createSchedule.ClassroomId);
                    return (null, "Classroom not found");
                }

                var sectionExists = await context.Sections.AnyAsync(s => s.Id == createSchedule.SectionId);
                if (!sectionExists)
                {
                    logger.LogWarning("Schedule creation failed: Section with ID {SectionId} not found", createSchedule.SectionId);
                    return (null, "Section not found");
                }

                var schedule = new Schedules
                {
                    TimeIn = createSchedule.TimeIn,
                    TimeOut = createSchedule.TimeOut,
                    DayOfWeek = createSchedule.DayOfWeek,
                    SubjectId = createSchedule.SubjectId,
                    ClassroomId = createSchedule.ClassroomId,
                    SectionId = createSchedule.SectionId,
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
                throw new ScheduleServiceException("CreateSchedule", "An error occurred while creating the schedule", ex);
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

                // Validate relationships if needed
                var subjectExists = await context.Subjects.AnyAsync(s => s.Id == updateSchedule.SubjectId);
                if (!subjectExists)
                {
                    logger.LogWarning("Schedule update failed: Subject with ID {SubjectId} not found", updateSchedule.SubjectId);
                    return (null, "Subject not found");
                }

                var classroomExists = await context.Classrooms.AnyAsync(c => c.Id == updateSchedule.ClassroomId);
                if (!classroomExists)
                {
                    logger.LogWarning("Schedule update failed: Classroom with ID {ClassroomId} not found", updateSchedule.ClassroomId);
                    return (null, "Classroom not found");
                }

                var sectionExists = await context.Sections.AnyAsync(s => s.Id == updateSchedule.SectionId);
                if (!sectionExists)
                {
                    logger.LogWarning("Schedule update failed: Section with ID {SectionId} not found", updateSchedule.SectionId);
                    return (null, "Section not found");
                }

                existingSchedule.TimeIn = updateSchedule.TimeIn;
                existingSchedule.TimeOut = updateSchedule.TimeOut;
                existingSchedule.DayOfWeek = updateSchedule.DayOfWeek;
                existingSchedule.SubjectId = updateSchedule.SubjectId;
                existingSchedule.ClassroomId = updateSchedule.ClassroomId;
                existingSchedule.SectionId = updateSchedule.SectionId;
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
                throw new ScheduleServiceException($"UpdateSchedule: {id}", "An error occurred while updating the schedule", ex);
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while deleting schedule with ID: {Id}", id);
                throw new ScheduleServiceException($"DeleteSchedule: {id}", "An error occurred while deleting the schedule", ex);
            }
        }
        
        #endregion
    }
}