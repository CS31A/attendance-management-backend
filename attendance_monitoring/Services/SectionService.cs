using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Services
{
    public class SectionService(ISectionRepository sectionRepository, ILogger<SectionService> logger)
        : ISectionService
    {
        public async Task<Section?> GetSectionByIdAsync(int sectionId)
        {
            try
            {
                return await sectionRepository.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving section with ID {SectionId} from repository.", sectionId);
                throw;
            }
        }

        public async Task<IEnumerable<SectionResponseDto>> GetAllSectionsAsync()
        {
            try
            {
                var sections = await sectionRepository.GetAllSectionsAsync().ConfigureAwait(false);
                return sections.Select(s => new SectionResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    InstructorId = s.InstructorId,
                    CourseId = s.CourseId,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all sections from repository.");
                throw;
            }
        }

        public async Task<SectionResponseDto?> CreateSectionAsync(Section section)
        {
            try
            {
                var createdSection = await sectionRepository.CreateSectionAsync(section).ConfigureAwait(false);
                
                return new SectionResponseDto
                {
                    Id = createdSection.Id,
                    Name = createdSection.Name,
                    InstructorId = createdSection.InstructorId,
                    CourseId = createdSection.CourseId,
                    CreatedAt = createdSection.CreatedAt,
                    UpdatedAt = createdSection.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating section with name {SectionName} in repository.", section.Name);
                throw;
            }
        }

        public async Task<SectionResponseDto?> UpdateSectionAsync(int id, Section section)
        {
            try
            {
                var updatedSection = await sectionRepository.UpdateSectionAsync(id, section).ConfigureAwait(false);
                if (updatedSection == null)
                {
                    logger.LogWarning("Section with ID {SectionId} not found for update in repository.", id);
                    return null;
                }

                return new SectionResponseDto
                {
                    Id = updatedSection.Id,
                    Name = updatedSection.Name,
                    InstructorId = updatedSection.InstructorId,
                    CourseId = updatedSection.CourseId,
                    CreatedAt = updatedSection.CreatedAt,
                    UpdatedAt = updatedSection.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating section with ID {SectionId} in repository.", id);
                throw;
            }
        }

        public async Task<bool> DeleteSectionAsync(int id)
        {
            try
            {
                return await sectionRepository.DeleteSectionAsync(id).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting section with ID {SectionId} from repository.", id);
                throw;
            }
        }

        public async Task<IEnumerable<Student>> GetActiveStudentsBySectionIdAsync(int sectionId)
        {
            try
            {
                return await sectionRepository.GetActiveStudentsBySectionIdAsync(sectionId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving active students for section with ID {SectionId} from repository.", sectionId);
                throw;
            }
        }

        public async Task<IEnumerable<Student>> GetAllStudentsBySectionIdAsync(int sectionId)
        {
            try
            {
                return await sectionRepository.GetAllStudentsBySectionIdAsync(sectionId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all students for section with ID {SectionId} from repository.", sectionId);
                throw;
            }
        }
    }
}