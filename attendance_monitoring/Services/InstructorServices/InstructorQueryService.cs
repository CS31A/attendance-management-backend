using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services.InstructorServices;

/// <summary>
/// Service for instructor profile, subject, schedule, and section queries
/// </summary>
public class InstructorQueryService : IInstructorQueryService
{
    private readonly IInstructorRepository _instructorRepository;
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<InstructorQueryService> _logger;

    public InstructorQueryService(
        IInstructorRepository instructorRepository,
        IScheduleRepository scheduleRepository,
        IUserContextService userContextService,
        ILogger<InstructorQueryService> logger)
    {
        _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region GetSubjectsByInstructorIdAsync
    /// <summary>
    /// Retrieves all subjects taught by a specific instructor
    /// </summary>
    /// <param name="instructorId">The ID of the instructor</param>
    /// <returns>A collection of subjects taught by the instructor</returns>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the instructor is not found</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
    public async Task<IEnumerable<SubjectResponseDto>> GetSubjectsByInstructorIdAsync(Guid instructorId)
    {
        try
        {
            _logger.LogInformation("Retrieving subjects for instructor ID: {InstructorId}", instructorId);

            // Verify instructor exists
            var instructor = await _instructorRepository.GetInstructorByIdAsync(instructorId).ConfigureAwait(false);
            if (instructor == null)
            {
                _logger.LogWarning("Instructor with ID {InstructorId} not found", instructorId);
                throw new EntityNotFoundException<Guid>("Instructor", instructorId);
            }

            // Get subjects from schedules
            var subjects = await _scheduleRepository.GetSubjectsByInstructorIdAsync(instructorId).ConfigureAwait(false);
            var subjectDtos = subjects.Select(s => new SubjectResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();

            _logger.LogInformation("Successfully retrieved {Count} subjects for instructor ID: {InstructorId}",
                subjectDtos.Count, instructorId);
            return subjectDtos;
        }
        catch (EntityNotFoundException<Guid>)
        {
            // Re-throw EntityNotFoundException as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving subjects for instructor ID: {InstructorId}", instructorId);
            throw new EntityServiceException("Instructor", $"GetSubjectsByInstructorId: {instructorId}",
                "An error occurred while retrieving instructor subjects", ex);
        }
    }
    #endregion

    #region GetSchedulesByInstructorAsync
    /// <summary>
    /// Retrieves all schedules for the current authenticated instructor
    /// </summary>
    /// <param name="userPrincipal">The claims principal of the current user</param>
    /// <returns>A collection of schedules for the instructor</returns>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.String}">Thrown when the instructor is not found</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
    public async Task<IEnumerable<ScheduleResponseDto>> GetSchedulesByInstructorAsync(ClaimsPrincipal userPrincipal)
    {
        try
        {
            _logger.LogInformation("Retrieving schedules for authenticated instructor");

            // Extract user ID from JWT claims
            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in JWT claims");
                throw new EntityNotFoundException<string>("User", userId ?? "null");
            }

            // Get instructor by user ID
            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                throw new EntityNotFoundException<string>("Instructor", $"UserId: {userId}");
            }

            // Get schedules for instructor
            var schedules = await _scheduleRepository.GetSchedulesByInstructorIdAsync(instructor.Id).ConfigureAwait(false);
            var scheduleDtos = schedules.Select(ScheduleServiceSupport.MapToResponseDto).ToList();

            _logger.LogInformation("Successfully retrieved {Count} schedules for instructor ID: {InstructorId}",
                scheduleDtos.Count, instructor.Id);
            return scheduleDtos;
        }
        catch (EntityNotFoundException<string>)
        {
            // Re-throw EntityNotFoundException as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving schedules for authenticated instructor");
            throw new EntityServiceException("Instructor", "GetSchedulesByInstructor",
                "An error occurred while retrieving instructor schedules", ex);
        }
    }
    #endregion

    #region GetInstructorProfileAsync
    /// <summary>
    /// Retrieves the instructor profile for the current authenticated user
    /// </summary>
    /// <param name="userPrincipal">The claims principal of the current user</param>
    /// <returns>The instructor profile if found, null otherwise</returns>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
    public async Task<InstructorProfileResponseDto?> GetInstructorProfileAsync(ClaimsPrincipal userPrincipal)
    {
        try
        {
            _logger.LogInformation("Retrieving instructor profile for authenticated user");

            // Extract user ID from JWT claims
            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in JWT claims");
                return null;
            }

            // Get instructor by user ID
            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                return null;
            }

            // Map to response DTO
            var profileDto = new InstructorProfileResponseDto
            {
                Id = instructor.Id,
                Firstname = instructor.Firstname,
                Lastname = instructor.Lastname,
                Department = instructor.Department,
                Email = instructor.User?.Email,
                CreatedAt = instructor.CreatedAt,
                UpdatedAt = instructor.UpdatedAt
            };

            _logger.LogInformation("Successfully retrieved instructor profile for user ID: {UserId}, instructor ID: {InstructorId}",
                userId, instructor.Id);
            return profileDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving instructor profile");
            throw new EntityServiceException("Instructor", "GetInstructorProfile",
                "An error occurred while retrieving the instructor profile", ex);
        }
    }
    #endregion

    #region GetSectionsWithStudentsByInstructorAsync
    /// <summary>
    /// Retrieves all sections with students for the current authenticated instructor
    /// </summary>
    /// <param name="userPrincipal">The claims principal of the current user</param>
    /// <returns>Instructor sections with students response DTO</returns>
    /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.String}">Thrown when the instructor is not found</exception>
    /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
    public async Task<InstructorSectionsWithStudentsResponseDto> GetSectionsWithStudentsByInstructorAsync(ClaimsPrincipal userPrincipal)
    {
        try
        {
            _logger.LogInformation("Retrieving sections with students for authenticated instructor");

            // Extract user ID from JWT claims
            var userId = await _userContextService.GetUserIdAsync(userPrincipal).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in JWT claims");
                throw new EntityNotFoundException<string>("User", userId ?? "null");
            }

            // Get instructor by user ID
            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                _logger.LogWarning("No instructor record found for user ID: {UserId}", userId);
                throw new EntityNotFoundException<string>("Instructor", $"UserId: {userId}");
            }

            _logger.LogInformation("Getting sections with students for instructor ID: {InstructorId}", instructor.Id);

            // Get schedules with related data from repository
            var schedules = await _instructorRepository.GetSchedulesWithRelatedDataByInstructorIdAsync(instructor.Id).ConfigureAwait(false);
            var schedulesList = schedules.ToList();

            if (schedulesList.Count == 0)
            {
                _logger.LogInformation("No schedules found for instructor ID: {InstructorId}", instructor.Id);
                return new InstructorSectionsWithStudentsResponseDto
                {
                    InstructorId = instructor.Id,
                    InstructorFirstname = instructor.Firstname ?? string.Empty,
                    InstructorLastname = instructor.Lastname ?? string.Empty,
                    Sections = new List<SectionWithStudentsDto>()
                };
            }

            // Group schedules by section
            var sectionGroups = schedulesList
                .GroupBy(s => s.SectionId)
                .ToList();

            var sectionDtos = new List<SectionWithStudentsDto>();

            foreach (var sectionGroup in sectionGroups)
            {
                var firstSchedule = sectionGroup.First();
                var section = firstSchedule.Section;
                var regularStudents = (await _instructorRepository.GetRegularStudentsBySectionIdAsync(section.Id).ConfigureAwait(false))
                    .Select(student => new StudentDto
                    {
                        StudentId = student.Id,
                        Firstname = student.Firstname,
                        Lastname = student.Lastname,
                        IsRegular = true,
                        EnrollmentType = EnrollmentTypeConstants.Regular
                    })
                    .ToList();

                // Group schedules by subject within this section
                var subjectSchedules = sectionGroup
                    .GroupBy(s => s.SubjectId)
                    .Select(subjectGroup =>
                    {
                        var schedule = subjectGroup.First();

                        // Regular students come from primary section membership.
                        // Irregular and retake students come from explicit additional enrollments.
                        var irregularStudents = section.StudentEnrollments
                            .Where(se => se.SubjectId == schedule.SubjectId
                                && !se.Student.IsDeleted
                                && se.IsActive
                                && se.Student.SectionId != section.Id)
                            .Select(se => new StudentDto
                            {
                                StudentId = se.Student.Id,
                                Firstname = se.Student.Firstname,
                                Lastname = se.Student.Lastname,
                                IsRegular = false,
                                EnrollmentType = se.EnrollmentType
                            })
                            .ToList();

                        var enrolledStudents = regularStudents
                            .Concat(irregularStudents)
                            .GroupBy(s => s.StudentId)
                            .Select(g => g.First())
                            .OrderBy(s => s.Lastname)
                            .ThenBy(s => s.Firstname)
                            .ToList();

                        return new SubjectScheduleDto
                        {
                            SubjectId = schedule.Subject.Id,
                            SubjectName = schedule.Subject.Name,
                            SubjectCode = schedule.Subject.Code,
                            ScheduleId = schedule.Id,
                            DayOfWeek = schedule.DayOfWeek,
                            TimeIn = schedule.TimeIn,
                            TimeOut = schedule.TimeOut,
                            ClassroomId = schedule.Classroom.Id,
                            ClassroomName = schedule.Classroom.Name,
                            Students = enrolledStudents
                        };
                    })
                    .OrderBy(s => s.SubjectName)
                    .ToList();

                sectionDtos.Add(new SectionWithStudentsDto
                {
                    SectionId = section.Id,
                    SectionName = section.Name,
                    CourseId = SectionHelper.GetRequiredCourse(section).Id,
                    CourseName = SectionHelper.GetRequiredCourse(section).Name,
                    Subjects = subjectSchedules
                });
            }

            var response = new InstructorSectionsWithStudentsResponseDto
            {
                InstructorId = instructor.Id,
                InstructorFirstname = instructor.Firstname ?? string.Empty,
                InstructorLastname = instructor.Lastname ?? string.Empty,
                Sections = sectionDtos.OrderBy(s => s.SectionName).ToList()
            };

            _logger.LogInformation("Successfully retrieved {SectionCount} sections with students for instructor ID: {InstructorId}",
                sectionDtos.Count, instructor.Id);

            return response;
        }
        catch (EntityNotFoundException<string>)
        {
            // Re-throw EntityNotFoundException as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving sections with students for authenticated instructor");
            throw new EntityServiceException("Instructor", "GetSectionsWithStudentsByInstructor",
                "An error occurred while retrieving sections with students", ex);
        }
    }
    #endregion
}
