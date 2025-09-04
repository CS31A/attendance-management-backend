using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.Services
{
    public class InstructorService : IInstructorService
    {
        private readonly IInstructorRepository _instructorRepository;
        private readonly UserContextService _userContextService;

        public InstructorService(IInstructorRepository instructorRepository, UserContextService userContextService)
        {
            _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        }

        public async Task<IEnumerable<Instructor>> GetAllInstructorsAsync(PaginationQuery paginationQuery)
        {
            return await _instructorRepository.GetAllInstructorsAsync(paginationQuery);
        }

        public async Task<Instructor?> GetInstructorByIdAsync(int id)
        {
            return await _instructorRepository.GetInstructorByIdAsync(id);
        }

        public async Task<(Instructor?, string?)> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal userPrincipal)
        {
            // Additional validation for defense in depth
            if (createInstructor == null)
            {
                return (null, "Create instructor data is required");
            }

            if (string.IsNullOrWhiteSpace(createInstructor.Firstname))
            {
                return (null, "First name is required");
            }

            if (string.IsNullOrWhiteSpace(createInstructor.Lastname))
            {
                return (null, "Last name is required");
            }

            if (string.IsNullOrWhiteSpace(createInstructor.Email))
            {
                return (null, "Email is required");
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                return (null, "User ID not found in token");
            }

            var existingInstructor = await _instructorRepository.GetInstructorByUserIdAsync(userId);
            if (existingInstructor != null)
            {
                return (null, "Instructor record already exists for this user");
            }

            var instructor = new Instructor
            {
                Firstname = createInstructor.Firstname,
                Lastname = createInstructor.Lastname,
                Email = createInstructor.Email,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdInstructor = await _instructorRepository.CreateInstructor(instructor);
            await _instructorRepository.SaveChangesAsync();

            return (createdInstructor, null);
        }

        public async Task<(Instructor?, string?)> UpdateInstructorAsync(int id, UpdateInstructor updateInstructor, ClaimsPrincipal userPrincipal)
        {
            // Additional validation for defense in depth
            if (updateInstructor == null)
            {
                return (null, "Update instructor data is required");
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                return (null, "User ID not found in token");
            }

            var existingInstructor = await _instructorRepository.GetInstructorByIdAsync(id);
            if (existingInstructor == null)
            {
                return (null, "Instructor not found");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingInstructor.UserId, "Admin", "Teacher");
            if (!isAuthorized)
            {
                return (null, "You are not authorized to update this instructor record.");
            }

            if (!string.IsNullOrEmpty(updateInstructor.Firstname))
            {
                existingInstructor.Firstname = updateInstructor.Firstname;
            }

            if (!string.IsNullOrEmpty(updateInstructor.Lastname))
            {
                existingInstructor.Lastname = updateInstructor.Lastname;
            }

            if (!string.IsNullOrEmpty(updateInstructor.Email))
            {
                existingInstructor.Email = updateInstructor.Email;
            }

            existingInstructor.UpdatedAt = DateTime.UtcNow;

            var updatedInstructor = await _instructorRepository.UpdateInstructorAsync(existingInstructor);
            await _instructorRepository.SaveChangesAsync();

            return (updatedInstructor, null);
        }

    }
}
