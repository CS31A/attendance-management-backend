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
        public async Task<Section?> GetSectionByIdAsync(Guid sectionId)
            => await context.Sections
                .AsNoTracking()
                .Include(section => section.Course)
                .FirstOrDefaultAsync(s => s.Id == sectionId)
                .ConfigureAwait(false);
        #endregion

        #region GetSectionByUuidAsync
        public async Task<Section?> GetSectionByUuidAsync(Guid id)
            => await context.Sections
                .AsNoTracking()
                .Include(section => section.Course)
                .FirstOrDefaultAsync(s => s.Id == id)
                .ConfigureAwait(false);
        #endregion

        #region GetSectionByUuidTrackedAsync
        public async Task<Section?> GetSectionByUuidTrackedAsync(Guid id)
            => await context.Sections
                .Include(section => section.Course)
                .FirstOrDefaultAsync(s => s.Id == id)
                .ConfigureAwait(false);
        #endregion

        #region GetAllSectionsAsync
        public async Task<IEnumerable<Section>> GetAllSectionsAsync()
            => await context.Sections
                .AsNoTracking()
                .Include(section => section.Course)
                .ToListAsync()
                .ConfigureAwait(false);
        #endregion

        #region GetActiveStudentsBySectionIdAsync
        public async Task<IEnumerable<Student>> GetActiveStudentsBySectionIdAsync(Guid sectionId)
            => await context.Students
                .Include(s => s.User)
                .AsNoTracking()
                .Where(s => s.SectionId == sectionId && !s.IsDeleted)
                .ToListAsync().ConfigureAwait(false);
        #endregion

        #region GetAllStudentsBySectionIdAsync
        public async Task<IEnumerable<Student>> GetAllStudentsBySectionIdAsync(Guid sectionId)
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
        public async Task<Section?> UpdateSectionAsync(Guid id, Section section)
        {
            var existingSection = await context.Sections
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Id == id)
                .ConfigureAwait(false);
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
        public async Task<bool> DeleteSectionAsync(Guid id)
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
        public async Task<bool> HasStudentsInSectionAsync(Guid sectionId)
        {
            return await context.Students
                .AsNoTracking()
                .AnyAsync(s => s.SectionId == sectionId)
                .ConfigureAwait(false);
        }
        #endregion

        #region HasStudentEnrollmentsInSectionAsync
        public async Task<bool> HasStudentEnrollmentsInSectionAsync(Guid sectionId)
        {
            return await context.StudentEnrollments
                .AsNoTracking()
                .AnyAsync(se => se.SectionId == sectionId)
                .ConfigureAwait(false);
        }
        #endregion

        #region HasSchedulesInSectionAsync
        public async Task<bool> HasSchedulesInSectionAsync(Guid sectionId)
        {
            return await context.Schedules
                .AsNoTracking()
                .AnyAsync(s => s.SectionId == sectionId)
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
