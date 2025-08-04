using attendance_monitoring.Models.Response;
using attendance_monitoring.Repositories;

namespace attendance_monitoring.Classes;

public class Login : ILoginRepository
{
    public Task<LoginModel> GetLogin()
    {
        throw new NotImplementedException();
    }

    public Task<object> Register()
    {
        throw new NotImplementedException();
    }
}