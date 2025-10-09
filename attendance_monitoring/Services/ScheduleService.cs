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
    public class ScheduleService : IScheduleService
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ScheduleService> _logger;

        public ScheduleService(IScheduleRepository scheduleRepository, ApplicationDbContext context, ILogger<ScheduleService> logger)
        {
            _scheduleRepository = scheduleRepository;
            _context = context;
            _logger = logger;
        }
        public async Task<IEnumerable<Schedules>> GetAllSchedulesAsync()
        {
            _logger.LogInformation("Retrieving all schedules");
            try
            {
                var schedules = await _scheduleRepository.GetAllSchedulesAsync();
                var allSchedules = schedules.ToList();
                _logger.LogInformation("Successfully retrieved {Count} schedules", allSchedules.Count);
                return allSchedules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving all schedules");
                throw new ScheduleServiceException("GetAllSchedules", "An error occurred while retrieving schedules", ex);
            }
        }

        public async Task<Schedules?> GetScheduleByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving schedule by ID: {Id}", id);
            try
            {
                var schedule = await _scheduleRepository.GetScheduleByIdAsync(id).ConfigureAwait(false);
                if (schedule == null)
                {
                    _logger.LogWarning("Schedule with ID {Id} not found", id);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved schedule with ID: {Id}", id);
                return schedule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving schedule with ID {Id}", id);
                throw new ScheduleServiceException($"GetScheduleById: {id}", "An error occurred while retrieving the schedule", ex);
            }
        }

        public async Task<(Schedules?, string?)> CreateScheduleAsync(CreateSchedule createSchedule)
        {
            _logger.LogInformation("Creating new schedule with TimeIn: {TimeIn} and TimeOut: {TimeOut}", 
                createSchedule.TimeIn, createSchedule.TimeOut);
            try
            {
                // Validate relationships if needed
                var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == createSchedule.SubjectId);
                if (!subjectExists)
                {
                    _logger.LogWarning("Schedule creation failed: Subject with ID {SubjectId} not found", createSchedule.SubjectId);
                    return (null, "Subject not found");
                }

                var classroomExists = await _context.Classrooms.AnyAsync(c => c.Id == createSchedule.ClassroomId);
                if (!classroomExists)
                {
                    _logger.LogWarning("Schedule creation failed: Classroom with ID {ClassroomId} not found", createSchedule.ClassroomId);
                    return (null, "Classroom not found");
                }

                var sectionExists = await _context.Sections.AnyAsync(s => s.Id == createSchedule.SectionId);
                if (!sectionExists)
                {
                    _logger.LogWarning("Schedule creation failed: Section with ID {SectionId} not found", createSchedule.SectionId);
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

                var createdSchedule = await _scheduleRepository.AddScheduleAsync(schedule);

                _logger.LogInformation("Successfully created schedule with ID: {Id}", createdSchedule.Id);
                return (createdSchedule, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating schedule");
                throw new ScheduleServiceException("CreateSchedule", "An error occurred while creating the schedule", ex);
            }
        }

        public async Task<(Schedules?, string?)> UpdateScheduleAsync(int id, UpdateSchedule updateSchedule)
        {
            _logger.LogInformation("Updating schedule with ID: {Id}", id);
            
            try
            {
                var existingSchedule = await _scheduleRepository.GetScheduleByIdAsync(id);
                if (existingSchedule == null)
                {
                    _logger.LogWarning("Schedule update failed: Schedule with ID {Id} not found", id);
                    return (null, "Schedule not found");
                }

                // Validate relationships if needed
                var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == updateSchedule.SubjectId);
                if (!subjectExists)
                {
                    _logger.LogWarning("Schedule update failed: Subject with ID {SubjectId} not found", updateSchedule.SubjectId);
                    return (null, "Subject not found");
                }

                var classroomExists = await _context.Classrooms.AnyAsync(c => c.Id == updateSchedule.ClassroomId);
                if (!classroomExists)
                {
                    _logger.LogWarning("Schedule update failed: Classroom with ID {ClassroomId} not found", updateSchedule.ClassroomId);
                    return (null, "Classroom not found");
                }

                var sectionExists = await _context.Sections.AnyAsync(s => s.Id == updateSchedule.SectionId);
                if (!sectionExists)
                {
                    _logger.LogWarning("Schedule update failed: Section with ID {SectionId} not found", updateSchedule.SectionId);
                    return (null, "Section not found");
                }

                existingSchedule.TimeIn = updateSchedule.TimeIn;
                existingSchedule.TimeOut = updateSchedule.TimeOut;
                existingSchedule.DayOfWeek = updateSchedule.DayOfWeek;
                existingSchedule.SubjectId = updateSchedule.SubjectId;
                existingSchedule.ClassroomId = updateSchedule.ClassroomId;
                existingSchedule.SectionId = updateSchedule.SectionId;
                existingSchedule.UpdatedAt = DateTime.UtcNow;

                var updatedSchedule = await _scheduleRepository.UpdateScheduleAsync(existingSchedule);
                
                if (updatedSchedule == null)
                {
                    _logger.LogWarning("Schedule update failed: Failed to update schedule with ID {Id}", id);
                    return (null, "Failed to update schedule");
                }
                
                _logger.LogInformation("Successfully updated schedule with ID: {Id}", updatedSchedule.Id);
                return (updatedSchedule, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating schedule with ID: {Id}", id);
                throw new ScheduleServiceException($"UpdateSchedule: {id}", "An error occurred while updating the schedule", ex);
            }
        }

        public async Task<string?> SoftDeleteScheduleAsync(int id, ClaimsPrincipal user)
        {
            // Soft delete is currently not implemented for schedules
            return "Soft delete is not implemented for schedules. Consider using hard delete instead.";
        }

        public async Task<string?> HardDeleteScheduleAsync(int id, ClaimsPrincipal user)
        {
            _logger.LogInformation("Deleting schedule with ID: {Id}", id);
            
            try
            {
                var existingSchedule = await _scheduleRepository.GetScheduleByIdAsync(id).ConfigureAwait(false);
                if (existingSchedule == null)
                {
                    _logger.LogWarning("Schedule deletion failed: Schedule with ID {Id} not found", id);
                    return "Schedule not found";
                }

                var result = await _scheduleRepository.DeleteScheduleAsync(id).ConfigureAwait(false);
                if (!result)
                {
                    _logger.LogWarning("Schedule deletion failed: Schedule with ID {Id} not found", id);
                    return "Schedule not found";
                }

                var rowsAffected = await _scheduleRepository.SaveChangesAsync().ConfigureAwait(false);
                if (rowsAffected == 0)
                {
                    _logger.LogWarning("Schedule deletion failed: Schedule with ID {Id} may have been deleted by another process", id);
                    return "Schedule may have been deleted by another process.";
                }
                
                _logger.LogInformation("Successfully deleted schedule with ID: {Id}", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting schedule with ID: {Id}", id);
                throw new ScheduleServiceException($"DeleteSchedule: {id}", "An error occurred while deleting the schedule", ex);
            }
        }

        public async Task<string?> RestoreScheduleAsync(int id, ClaimsPrincipal user)
        {
            // Restore is currently not implemented for schedules
            return "Restore functionality not applicable without soft delete implementation";
        }
        
    }
}