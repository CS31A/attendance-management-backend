using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace attendance_monitoring.Repositories
{
    public class SectionRepository : ISectionRepository
    {
        private readonly ApplicationDbContext _context;

        public SectionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Section?> GetSectionByIdAsync(int sectionId)
        {
            return await _context.Sections.FindAsync(sectionId);
        }

        public async Task<IEnumerable<Section>> GetAllSectionsAsync()
        {
            return await _context.Sections.ToListAsync();
        }

        public async Task<Section> CreateSectionAsync(Section section)
        {
            section.CreatedAt = DateTime.UtcNow;
            section.UpdatedAt = DateTime.UtcNow;
            
            _context.Sections.Add(section);
            await _context.SaveChangesAsync();
            
            return section;
        }

        public async Task<Section?> UpdateSectionAsync(int id, Section section)
        {
            var existingSection = await _context.Sections.FindAsync(id);
            if (existingSection == null)
            {
                return null;
            }

            existingSection.Name = section.Name;
            existingSection.InstructorId = section.InstructorId;
            existingSection.CourseId = section.CourseId;
            existingSection.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return existingSection;
        }

        public async Task<bool> DeleteSectionAsync(int id)
        {
            var section = await _context.Sections.FindAsync(id);
            if (section == null)
            {
                return false;
            }

            _context.Sections.Remove(section);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}