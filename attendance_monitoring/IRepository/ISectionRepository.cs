using attendance_monitoring.Classes;
using System.Threading.Tasks;

namespace attendance_monitoring.IRepository
{
    public interface ISectionRepository
    {
        Task<Section?> GetSectionByIdAsync(int sectionId);
    }
}