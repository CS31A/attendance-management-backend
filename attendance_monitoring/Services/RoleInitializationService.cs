using System.Threading.Tasks;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using Microsoft.Extensions.Logging;

namespace attendance_monitoring.Services
{
    public class RoleInitializationService : IRoleInitializationService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<RoleInitializationService> _logger;

        public RoleInitializationService(IAccountRepository accountRepository, ILogger<RoleInitializationService> logger)
        {
            _accountRepository = accountRepository;
            _logger = logger;
        }

        public async Task InitializeRolesAsync()
        {
            _logger.LogInformation("Initializing roles...");

            var validRoles = new[] { "Admin", "Teacher", "Student" };
            await _accountRepository.EnsureRolesExistAsync(validRoles);

            _logger.LogInformation("Roles initialized successfully.");
        }
    }
}
