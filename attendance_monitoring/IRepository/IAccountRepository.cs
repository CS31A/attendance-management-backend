using attendance_monitoring.Classes;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.IRepository
{
    public interface IAccountRepository
    {
        Task<IdentityUser?> FindUserByIdAsync(string id);
        Task<IdentityUser?> FindUserByUsernameAsync(string username);
        Task<IdentityUser?> FindUserByEmailAsync(string email);
        Task<IdentityResult> CreateUserAsync(IdentityUser user, string password);
        Task<SignInResult> CheckPasswordAsync(IdentityUser user, string password);
        Task EnsureRolesExistAsync(IEnumerable<string> roles);
        Task AddUserToRoleAsync(IdentityUser user, string role);
        Task<IList<string>> GetUserRolesAsync(IdentityUser user);
        Task CreateStudentProfileAsync(Student student);
        Task CreateInstructorProfileAsync(Instructor instructor);
    }
}
