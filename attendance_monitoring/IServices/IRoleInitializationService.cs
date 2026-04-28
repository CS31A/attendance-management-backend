using System.Threading.Tasks;

namespace attendance_monitoring.IServices
{
    public interface IRoleInitializationService
    {
        Task InitializeRolesAsync();
    }
}