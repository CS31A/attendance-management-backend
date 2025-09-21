using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountRepository(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IdentityUser?> FindUserByIdAsync(string id)
        {
            return await _userManager.FindByIdAsync(id).ConfigureAwait(false);
        }

        public async Task<IdentityUser?> FindUserByUsernameAsync(string username)
        {
            return await _userManager.FindByNameAsync(username).ConfigureAwait(false);
        }

        public async Task<IdentityUser?> FindUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        }

        public async Task<IdentityResult> CreateUserAsync(IdentityUser user, string password)
        {
            return await _userManager.CreateAsync(user, password).ConfigureAwait(false);
        }

        public async Task<SignInResult> CheckPasswordAsync(IdentityUser user, string password)
        {
            return await _signInManager.CheckPasswordSignInAsync(user, password, false).ConfigureAwait(false);
        }

        public async Task EnsureRolesExistAsync(IEnumerable<string> roles)
        {
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role).ConfigureAwait(false))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role)).ConfigureAwait(false);
                }
            }
        }

        public async Task AddUserToRoleAsync(IdentityUser user, string role)
        {
            await _userManager.AddToRoleAsync(user, role).ConfigureAwait(false);
        }

        public async Task<IList<string>> GetUserRolesAsync(IdentityUser user)
        {
            return await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        }

        public async Task CreateStudentProfileAsync(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task CreateInstructorProfileAsync(Instructor instructor)
        {
            _context.Instructors.Add(instructor);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        
        public async Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash).ConfigureAwait(false);
        }
    }
}
