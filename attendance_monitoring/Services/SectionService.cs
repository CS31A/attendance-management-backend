using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
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
        public async Task<Section> GetSectionByIdAsync(int sectionId)
        {
            try
            {
                var section = await sectionRepository.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
                if (section == null)
                {
                    logger.LogWarning("Section with ID {SectionId} not found", sectionId);
                    throw new EntityNotFoundException<int>("Section", sectionId);
                }

                logger.LogInformation("Successfully retrieved section with ID: {SectionId}", sectionId);
                return section;
            }
            catch (EntityNotFoundException<int>)
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
                    CourseId = s.CourseId,
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

                var sectionDto = new SectionResponseDto
                {
                    Id = createdSection.Id,
                    Name = createdSection.Name,
                    CourseId = createdSection.CourseId,
                    CreatedAt = createdSection.CreatedAt,
                    UpdatedAt = createdSection.UpdatedAt
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
        public async Task<SectionResponseDto> UpdateSectionAsync(int id, Section section)
        {
            try
            {
                logger.LogInformation("Updating section with ID: {SectionId}", id);
                var updatedSection = await sectionRepository.UpdateSectionAsync(id, section).ConfigureAwait(false);
                if (updatedSection == null)
                {
                    logger.LogWarning("Section with ID {SectionId} not found for update in repository.", id);
                    throw new EntityNotFoundException<int>("Section", id);
                }

                await sectionRepository.SaveChangesAsync().ConfigureAwait(false);

                var sectionDto = new SectionResponseDto
                {
                    Id = updatedSection.Id,
                    Name = updatedSection.Name,
                    CourseId = updatedSection.CourseId,
                    CreatedAt = updatedSection.CreatedAt,
                    UpdatedAt = updatedSection.UpdatedAt
                };

                logger.LogInformation("Successfully updated section with ID: {SectionId}", id);
                return sectionDto;
            }
            catch (EntityNotFoundException<int>)
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

        #endregion

        #region Delete Operations
        /// <summary>
        /// Deletes a section by ID
        /// </summary>
        /// <param name="id">The ID of the section to delete</param>
        /// <exception cref="T:attendance_monitoring.Exceptions.EntityNotFoundException{System.Int32}">Thrown when the section is not found</exception>
        /// <exception cref="EntityServiceException">Thrown when section deletion fails</exception>
        public async Task DeleteSectionAsync(int id)
        {
            try
            {
                logger.LogInformation("Deleting section with ID: {SectionId}", id);
                var result = await sectionRepository.DeleteSectionAsync(id).ConfigureAwait(false);
                if (!result)
                {
                    logger.LogWarning("Section with ID {SectionId} not found for deletion", id);
                    throw new EntityNotFoundException<int>("Section", id);
                }

                await sectionRepository.SaveChangesAsync().ConfigureAwait(false);
                logger.LogInformation("Successfully deleted section with ID: {SectionId}", id);
            }
            catch (EntityNotFoundException<int>)
            {
                // Re-throw EntityNotFoundException as-is
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting section with ID {SectionId} from repository.", id);
                throw new EntityServiceException("Section", $"DeleteSection: {id}", "An error occurred while deleting the section", ex);
            }
        }

        #endregion

        #region Get Operations (Additional)
        /// <summary>
        /// Retrieves active students by section ID
        /// </summary>
        /// <param name="sectionId">The ID of the section</param>
        /// <returns>A collection of active students</returns>
        /// <exception cref="EntityServiceException">Thrown when an error occurs during retrieval</exception>
        public async Task<IEnumerable<Student>> GetActiveStudentsBySectionIdAsync(int sectionId)
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
        public async Task<IEnumerable<Student>> GetAllStudentsBySectionIdAsync(int sectionId)
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
    }
}