using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace attendance_monitoring.Services
{
    public class SectionService(ISectionRepository sectionRepository)
        : ISectionService
    {
        public async Task<Section?> GetSectionByIdAsync(int sectionId)
        {
            return await sectionRepository.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
        }

        public async Task<IEnumerable<SectionResponseDto>> GetAllSectionsAsync()
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

        public async Task<SectionResponseDto?> CreateSectionAsync(Section section)
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

        public async Task<SectionResponseDto?> UpdateSectionAsync(int id, Section section)
        {
            var updatedSection = await sectionRepository.UpdateSectionAsync(id, section).ConfigureAwait(false);
            if (updatedSection == null)
            {
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

        public async Task<bool> DeleteSectionAsync(int id)
        {
            return await sectionRepository.DeleteSectionAsync(id).ConfigureAwait(false);
        }
    }
}