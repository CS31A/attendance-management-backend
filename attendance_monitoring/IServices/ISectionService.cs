using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices
{
    public interface ISectionService
    {
        Task<Section?> GetSectionByIdAsync(int sectionId);
        Task<IEnumerable<SectionResponseDto>> GetAllSectionsAsync();
        Task<SectionResponseDto?> CreateSectionAsync(Section section);
        Task<SectionResponseDto?> UpdateSectionAsync(int id, Section section);
        Task<bool> DeleteSectionAsync(int id);
    }
}