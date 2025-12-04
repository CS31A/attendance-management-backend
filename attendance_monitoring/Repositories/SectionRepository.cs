using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories
{
    public class SectionRepository(ApplicationDbContext context, ILogger<SectionRepository> logger) : ISectionRepository
    {
        #region Read Operations

        #region GetSectionByIdAsync
        public async Task<Section?> GetSectionByIdAsync(int sectionId)
        {
            try
            {
                return await context.Sections.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sectionId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving section with ID {SectionId} from database.", sectionId);
                throw;
            }
        }
        #endregion

        #region GetAllSectionsAsync
        public async Task<IEnumerable<Section>> GetAllSectionsAsync()
        {
            try
            {
                return await context.Sections.AsNoTracking().ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all sections from database.");
                throw;
            }
        }
        #endregion

        #region GetActiveStudentsBySectionIdAsync
        public async Task<IEnumerable<Student>> GetActiveStudentsBySectionIdAsync(int sectionId)
        {
            try
            {
                return await context.Students
                    .Include(s => s.User)
                    .AsNoTracking()
                    .Where(s => s.SectionId == sectionId && !s.IsDeleted)
                    .ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving active students for section with ID {SectionId} from database.", sectionId);
                throw;
            }
        }
        #endregion

        #region GetAllStudentsBySectionIdAsync
        public async Task<IEnumerable<Student>> GetAllStudentsBySectionIdAsync(int sectionId)
        {
            try
            {
                return await context.Students
                    .Include(s => s.User)
                    .AsNoTracking()
                    .Where(s => s.SectionId == sectionId)
                    .ToListAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving all students for section with ID {SectionId} from database.", sectionId);
                throw;
            }
        }
        #endregion

        #endregion

        #region Write Operations

        #region CreateSectionAsync
        public Task<Section> CreateSectionAsync(Section section)
        {
            try
            {
                section.CreatedAt = DateTime.UtcNow;
                section.UpdatedAt = DateTime.UtcNow;

                context.Sections.Add(section);

                return Task.FromResult(section);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while creating section with name {SectionName} in database.", section.Name);
                throw;
            }
        }
        #endregion

        #region UpdateSectionAsync
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
        #endregion

        #region DeleteSectionAsync
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
}
