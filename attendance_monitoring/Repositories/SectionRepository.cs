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
            => await context.Sections.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sectionId).ConfigureAwait(false);
        #endregion

        #region GetAllSectionsAsync
        public async Task<IEnumerable<Section>> GetAllSectionsAsync()
            => await context.Sections.AsNoTracking().ToListAsync().ConfigureAwait(false);
        #endregion

        #region GetActiveStudentsBySectionIdAsync
        public async Task<IEnumerable<Student>> GetActiveStudentsBySectionIdAsync(int sectionId)
            => await context.Students
                .Include(s => s.User)
                .AsNoTracking()
                .Where(s => s.SectionId == sectionId && !s.IsDeleted)
                .ToListAsync().ConfigureAwait(false);
        #endregion

        #region GetAllStudentsBySectionIdAsync
        public async Task<IEnumerable<Student>> GetAllStudentsBySectionIdAsync(int sectionId)
            => await context.Students
                .Include(s => s.User)
                .AsNoTracking()
                .Where(s => s.SectionId == sectionId)
                .ToListAsync().ConfigureAwait(false);
        #endregion

        #endregion

        #region Write Operations

        #region CreateSectionAsync
        public Task<Section> CreateSectionAsync(Section section)
        {
            section.CreatedAt = DateTime.UtcNow;
            section.UpdatedAt = DateTime.UtcNow;

            context.Sections.Add(section);

            return Task.FromResult(section);
        }
        #endregion

        #region UpdateSectionAsync
        public async Task<Section?> UpdateSectionAsync(int id, Section section)
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
        #endregion

        #region DeleteSectionAsync
        public async Task<bool> DeleteSectionAsync(int id)
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
        #endregion

        #endregion

        #region Dependency Check Operations

        #region HasStudentsInSectionAsync
        public async Task<bool> HasStudentsInSectionAsync(int sectionId)
        {
            return await context.Students
                .AsNoTracking()
                .AnyAsync(s => s.SectionId == sectionId)
                .ConfigureAwait(false);
        }
        #endregion

        #region HasStudentEnrollmentsInSectionAsync
        public async Task<bool> HasStudentEnrollmentsInSectionAsync(int sectionId)
        {
            return await context.StudentEnrollments
                .AsNoTracking()
                .AnyAsync(se => se.SectionId == sectionId)
                .ConfigureAwait(false);
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
