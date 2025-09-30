using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;

namespace attendance_monitoring.Services
{
    public class RoleInitializationService(
        IAccountRepository accountRepository,
        ILogger<RoleInitializationService> logger)
        : IRoleInitializationService
    {
        public async Task InitializeRolesAsync()
        {
            logger.LogInformation("Initializing roles...");

            var validRoles = new[] { "Admin", "Teacher", "Student" };
            await accountRepository.EnsureRolesExistAsync(validRoles).ConfigureAwait(false);

            logger.LogInformation("Roles initialized successfully.");
        }
    }
}
