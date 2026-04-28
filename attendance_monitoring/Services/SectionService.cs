using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Services
{
    public class SectionService(ISectionRepository sectionRepository, ILogger<SectionService> logger)
        : ISectionService
    {
        #region Get Operations
        /// <summary>
        /// Retrieves a specific section by ID
        /// </summary>
        /// <param name="sectionId">The ID of the section to retrieve</param>
        /// <returns>The section with the specified ID</returns>
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the section is not found</exception>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<Section> GetSectionByIdAsync(Guid sectionId)
        {
            try
            {
                var section = await sectionRepository.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
                if (section == null)
                {
                    logger.LogWarning("Section with ID {SectionId} not found", sectionId);
                    throw new EntityNotFoundException<Guid>("Section", sectionId);
                }

                logger.LogInformation("Successfully retrieved section with ID: {SectionId}", sectionId);
                return section;
            }
            catch (EntityNotFoundException<Guid>)
            {
                // Re-throw EntityNotFoundException as-is
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving section with ID {SectionId} from repository.", sectionId);
                throw new EntityServiceException("Section", $"GetSectionById: {sectionId}", "An error occurred while retrieving the section", ex);
            }
        }

        public async Task<Section> GetSectionByUuidAsync(Guid id)
        {
            try
            {
                var section = await sectionRepository.GetSectionByUuidAsync(id).ConfigureAwait(false);
                if (section == null)
                {
                    logger.LogWarning("Section with UUID {SectionId} not found", id);
                    throw new EntityNotFoundException<Guid>("Section", id);
                }

                logger.LogInformation("Successfully retrieved section with UUID: {SectionId}", id);
                return section;
            }
            catch (EntityNotFoundException<Guid>)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving section with UUID {SectionId} from repository.", id);
                throw new EntityServiceException("Section", $"GetSectionByUuid: {id}", "An error occurred while retrieving the section", ex);
            }
        }

        /// <summary>
        /// Retrieves all sections
        /// </summary>
        /// <returns>A collection of section response DTOs</returns>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<IEnumerable<SectionResponseDto>> GetAllSectionsAsync()
        {
            try
            {
                logger.LogInformation("Retrieving all sections");
                var sections = await sectionRepository.GetAllSectionsAsync().ConfigureAwait(false);
                var sectionDtos = sections.Select(s => new SectionResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    CourseId = s.Course?.Id,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }).ToList();

                logger.LogInformation("Successfully retrieved {Count} sections", sectionDtos.Count);
                return sectionDtos;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all sections from repository.");
                throw new EntityServiceException("Section", "GetAllSections", "An error occurred while retrieving sections", ex);
            }
        }

        #endregion

        #region Create Operations
        /// <summary>
        /// Creates a new section
        /// </summary>
        /// <param name="section">The section to create</param>
        /// <returns>The created section response DTO</returns>
        /// <exception cref="EntityServiceException">Thrown when section creation fails</exception>
        public async Task<SectionResponseDto> CreateSectionAsync(Section section)
        {
            try
            {
                logger.LogInformation("Creating new section with name: {SectionName}", section.Name);
                var createdSection = await sectionRepository.CreateSectionAsync(section).ConfigureAwait(false);
                await sectionRepository.SaveChangesAsync().ConfigureAwait(false);

                var refreshedSection = await sectionRepository.GetSectionByIdAsync(createdSection.Id).ConfigureAwait(false);
                if (refreshedSection == null)
                {
                    logger.LogError("Created section with ID {SectionId} could not be found after save.", createdSection.Id);
                    throw new InvalidOperationException("Created section could not be reloaded.");
                }

                var sectionDto = new SectionResponseDto
                {
                    Id = refreshedSection.Id,
                    Name = refreshedSection.Name,
                    CourseId = refreshedSection.Course?.Id,
                    CreatedAt = refreshedSection.CreatedAt,
                    UpdatedAt = refreshedSection.UpdatedAt
                };

                logger.LogInformation("Successfully created section with ID: {SectionId}", createdSection.Id);
                return sectionDto;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating section with name {SectionName} in repository.", section.Name);
                throw new EntityServiceException("Section", "CreateSection", "An error occurred while creating the section", ex);
            }
        }

        #endregion

        #region Update Operations
        /// <summary>
        /// Updates an existing section
        /// </summary>
        /// <param name="id">The ID of the section to update</param>
        /// <param name="section">The updated section data</param>
        /// <returns>The updated section response DTO</returns>
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the section is not found</exception>
        /// <exception cref="EntityServiceException">Thrown when section update fails</exception>
        public async Task<SectionResponseDto> UpdateSectionAsync(Guid id, Section section)
        {
            try
            {
                logger.LogInformation("Updating section with ID: {SectionId}", id);
                var updatedSection = await sectionRepository.UpdateSectionAsync(id, section).ConfigureAwait(false);
                if (updatedSection == null)
                {
                    logger.LogWarning("Section with ID {SectionId} not found for update in repository.", id);
                    throw new EntityNotFoundException<Guid>("Section", id);
                }

                await sectionRepository.SaveChangesAsync().ConfigureAwait(false);

                var refreshedSection = await sectionRepository.GetSectionByIdAsync(updatedSection.Id).ConfigureAwait(false);
                if (refreshedSection == null)
                {
                    logger.LogError("Updated section with ID {SectionId} could not be found after save.", updatedSection.Id);
                    throw new InvalidOperationException("Updated section could not be reloaded.");
                }

                var sectionDto = new SectionResponseDto
                {
                    Id = refreshedSection.Id,
                    Name = refreshedSection.Name,
                    CourseId = refreshedSection.Course?.Id,
                    CreatedAt = refreshedSection.CreatedAt,
                    UpdatedAt = refreshedSection.UpdatedAt
                };

                logger.LogInformation("Successfully updated section with ID: {SectionId}", id);
                return sectionDto;
            }
            catch (EntityNotFoundException<Guid>)
            {
                // Re-throw EntityNotFoundException as-is
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating section with ID {SectionId} in repository.", id);
                throw new EntityServiceException("Section", $"UpdateSection: {id}", "An error occurred while updating the section", ex);
            }
        }

        public async Task<SectionResponseDto> UpdateSectionByUuidAsync(Guid id, Section section)
        {
            var existingSection = await GetSectionByUuidAsync(id).ConfigureAwait(false);
            return await UpdateSectionAsync(existingSection.Id, section).ConfigureAwait(false);
        }

        #endregion

        #region Delete Operations
        /// <summary>
        /// Deletes a section by ID
        /// </summary>
        /// <param name="id">The ID of the section to delete</param>
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the section is not found</exception>
        /// <exception cref="EntityConflictException">Thrown when section deletion is blocked by existing dependencies</exception>
        /// <exception cref="EntityServiceException">Thrown when section deletion fails unexpectedly</exception>
        public async Task DeleteSectionAsync(Guid id)
        {
            try
            {
                logger.LogInformation("Deleting section with ID: {SectionId}", id);
                var result = await sectionRepository.DeleteSectionAsync(id).ConfigureAwait(false);
                if (!result)
                {
                    logger.LogWarning("Section with ID {SectionId} not found for deletion", id);
                    throw new EntityNotFoundException<Guid>("Section", id);
                }

                await sectionRepository.SaveChangesAsync().ConfigureAwait(false);
                logger.LogInformation("Successfully deleted section with ID: {SectionId}", id);
            }
            catch (EntityNotFoundException<Guid>)
            {
                // Re-throw EntityNotFoundException as-is
                throw;
            }
            catch (DbUpdateException ex) when (ExceptionHandlingHelper.IsForeignKeyViolation(ex))
            {
                // Translate FK constraint violations into EntityConflictException
                var conflictMessage = ResolveConflictMessage(ex);
                logger.LogWarning(ex, "Cannot delete section {SectionId}: {ConflictMessage}", id, conflictMessage);
                throw new EntityConflictException("Section", ResolveConflictType(ex), conflictMessage, ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting section with ID {SectionId} from repository.", id);
                throw new EntityServiceException("Section", $"DeleteSection: {id}", "An error occurred while deleting the section", ex);
            }
        }

        public async Task DeleteSectionByUuidAsync(Guid id)
        {
            var existingSection = await GetSectionByUuidAsync(id).ConfigureAwait(false);
            await DeleteSectionAsync(existingSection.Id).ConfigureAwait(false);
        }

        /// <summary>
        /// Resolves the type of conflict based on the exception details.
        /// </summary>
        private static string ResolveConflictType(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;

            if (message.Contains("schedule", StringComparison.OrdinalIgnoreCase))
                return "schedules";
            if (message.Contains("enrollment", StringComparison.OrdinalIgnoreCase))
                return "enrollments";
            if (message.Contains("student", StringComparison.OrdinalIgnoreCase))
                return "students";

            return "dependencies";
        }

        /// <summary>
        /// Generates a user-friendly conflict message based on the exception details.
        /// </summary>
        private static string ResolveConflictMessage(DbUpdateException ex)
        {
            var message = ex.InnerException?.Message ?? ex.Message;

            if (message.Contains("schedule", StringComparison.OrdinalIgnoreCase))
                return "Cannot delete: Section has schedules assigned. Remove schedules first.";
            if (message.Contains("enrollment", StringComparison.OrdinalIgnoreCase))
                return "Cannot delete: Section has student enrollments. Remove enrollments first.";
            if (message.Contains("student", StringComparison.OrdinalIgnoreCase))
                return "Cannot delete: Section has assigned students. Reassign students first.";

            return "Cannot delete: Section has dependencies that prevent deletion.";
        }

        #endregion

        #region Get Operations (Additional)
        /// <summary>
        /// Retrieves active students by section ID
        /// </summary>
        /// <param name="sectionId">The ID of the section</param>
        /// <returns>A collection of active students</returns>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<IEnumerable<Student>> GetActiveStudentsBySectionIdAsync(Guid sectionId)
        {
            try
            {
                logger.LogInformation("Retrieving active students for section with ID: {SectionId}", sectionId);
                var students = await sectionRepository.GetActiveStudentsBySectionIdAsync(sectionId).ConfigureAwait(false);
                var studentList = students.ToList();
                logger.LogInformation("Successfully retrieved {Count} active students for section with ID: {SectionId}", studentList.Count, sectionId);
                return studentList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving active students for section with ID {SectionId} from repository.", sectionId);
                throw new EntityServiceException("Section", $"GetActiveStudentsBySectionId: {sectionId}", "An error occurred while retrieving active students", ex);
            }
        }

        /// <summary>
        /// Retrieves all students by section ID
        /// </summary>
        /// <param name="sectionId">The ID of the section</param>
        /// <returns>A collection of all students</returns>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<IEnumerable<Student>> GetAllStudentsBySectionIdAsync(Guid sectionId)
        {
            try
            {
                logger.LogInformation("Retrieving all students for section with ID: {SectionId}", sectionId);
                var students = await sectionRepository.GetAllStudentsBySectionIdAsync(sectionId).ConfigureAwait(false);
                var studentList = students.ToList();
                logger.LogInformation("Successfully retrieved {Count} students for section with ID: {SectionId}", studentList.Count, sectionId);
                return studentList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all students for section with ID {SectionId} from repository.", sectionId);
                throw new EntityServiceException("Section", $"GetAllStudentsBySectionId: {sectionId}", "An error occurred while retrieving students", ex);
            }
        }
        #endregion

        #region Dependency Check Operations
        public async Task<bool> HasStudentsInSectionAsync(Guid sectionId)
        {
            try
            {
                logger.LogInformation("Checking if section {SectionId} has students", sectionId);
                var hasStudents = await sectionRepository.HasStudentsInSectionAsync(sectionId).ConfigureAwait(false);
                logger.LogInformation("Section {SectionId} has students: {HasStudents}", sectionId, hasStudents);
                return hasStudents;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking if section {SectionId} has students", sectionId);
                throw new EntityServiceException("Section", $"HasStudentsInSection: {sectionId}", "Error checking section dependencies", ex);
            }
        }

        public async Task<bool> HasStudentEnrollmentsInSectionAsync(Guid sectionId)
        {
            try
            {
                logger.LogInformation("Checking if section {SectionId} has student enrollments", sectionId);
                var hasEnrollments = await sectionRepository.HasStudentEnrollmentsInSectionAsync(sectionId).ConfigureAwait(false);
                logger.LogInformation("Section {SectionId} has student enrollments: {HasEnrollments}", sectionId, hasEnrollments);
                return hasEnrollments;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking if section {SectionId} has student enrollments", sectionId);
                throw new EntityServiceException("Section", $"HasStudentEnrollmentsInSection: {sectionId}", "Error checking section dependencies", ex);
            }
        }

        public async Task<bool> HasSchedulesInSectionAsync(Guid sectionId)
        {
            try
            {
                logger.LogInformation("Checking if section {SectionId} has schedules", sectionId);
                var hasSchedules = await sectionRepository.HasSchedulesInSectionAsync(sectionId).ConfigureAwait(false);
                logger.LogInformation("Section {SectionId} has schedules: {HasSchedules}", sectionId, hasSchedules);
                return hasSchedules;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking if section {SectionId} has schedules", sectionId);
                throw new EntityServiceException("Section", $"HasSchedulesInSection: {sectionId}", "Error checking section dependencies", ex);
            }
        }
        #endregion
    }
}
