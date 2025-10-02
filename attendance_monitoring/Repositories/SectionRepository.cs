using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace attendance_monitoring.Repositories
{
    public class SectionRepository(ApplicationDbContext context, ILogger<SectionRepository> logger) : ISectionRepository
    {
        public async Task<Section?> GetSectionByIdAsync(int sectionId)
        {
            try
            {
                return await context.Sections.FindAsync(sectionId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving section with ID {SectionId} from database.", sectionId);
                throw;
            }
        }

        public async Task<IEnumerable<Section>> GetAllSectionsAsync()
        {
            try
            {
                return await context.Sections.ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all sections from database.");
                throw;
            }
        }

        public async Task<Section> CreateSectionAsync(Section section)
        {
            try
            {
                section.CreatedAt = DateTime.UtcNow;
                section.UpdatedAt = DateTime.UtcNow;
                
                context.Sections.Add(section);
                
                return section;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating section with name {SectionName} in database.", section.Name);
                throw;
            }
        }

        public async Task<Section?> UpdateSectionAsync(int id, Section section)
        {
            try
            {
                var existingSection = await context.Sections.FindAsync(id).ConfigureAwait(false);
                if (existingSection == null)
                {
                    logger.LogWarning("Section with ID {SectionId} not found for update in database.", id);
                    return null;
                }

                existingSection.Name = section.Name;
                existingSection.InstructorId = section.InstructorId;
                existingSection.CourseId = section.CourseId;
                existingSection.UpdatedAt = DateTime.UtcNow;

                return existingSection;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating section with ID {SectionId} in database.", id);
                throw;
            }
        }

        public async Task<bool> DeleteSectionAsync(int id)
        {
            try
            {
                var section = await context.Sections.FindAsync(id).ConfigureAwait(false);
                if (section == null)
                {
                    logger.LogWarning("Section with ID {SectionId} not found for deletion in database.", id);
                    return false;
                }

                context.Sections.Remove(section);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while deleting section with ID {SectionId} from database.", id);
                throw;
            }
        }

        public async Task<IEnumerable<Student>> GetActiveStudentsBySectionIdAsync(int sectionId)
        {
            try
            {
                return await context.Students
                    .Where(s => s.SectionId == sectionId && !s.IsDeleted)
                    .ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving active students for section with ID {SectionId} from database.", sectionId);
                throw;
            }
        }

        public async Task<IEnumerable<Student>> GetAllStudentsBySectionIdAsync(int sectionId)
        {
            try
            {
                return await context.Students
                    .Where(s => s.SectionId == sectionId)
                    .ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all students for section with ID {SectionId} from database.", sectionId);
                throw;
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
