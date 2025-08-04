using attendance_monitoring.Models.Response;

namespace attendance_monitoring.Repositories;

public interface ILoginRepository
{
    public Task<LoginModel> GetLogin();
    public Task<object> Register();
}