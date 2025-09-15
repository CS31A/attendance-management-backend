using attendance_monitoring.Classes;
using System.Threading.Tasks;

namespace attendance_monitoring.IRepository
{
    public interface ISectionRepository
    {
        Task<Section?> GetSectionByIdAsync(int sectionId);
        Task<IEnumerable<Section>> GetAllSectionsAsync();
        Task<Section> CreateSectionAsync(Section section);
        Task<Section?> UpdateSectionAsync(int id, Section section);
        Task<bool> DeleteSectionAsync(int id);
    }
}