using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.Services
{
    /// <summary>
    /// Service class for managing instructor-related operations
    /// </summary>
    public class InstructorService : IInstructorService
    {
        private readonly IInstructorRepository _instructorRepository;
        private readonly UserContextService _userContextService;

        /// <summary>
        /// Initializes a new instance of the InstructorService class
        /// </summary>
        /// <param name="instructorRepository">Repository for instructor data operations</param>
        /// <param name="userContextService">Service for managing user context and authorization</param>
        public InstructorService(IInstructorRepository instructorRepository, UserContextService userContextService)
        {
            _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        }

        /// <summary>
        /// Retrieves all instructors with pagination support
        /// </summary>
        /// <param name="paginationQuery">Pagination parameters</param>
        /// <returns>A collection of instructors</returns>
        public async Task<IEnumerable<Instructor>> GetAllInstructorsAsync(PaginationQuery paginationQuery)
        {
            return await _instructorRepository.GetAllInstructorsAsync(paginationQuery);
        }

        /// <summary>
        /// Retrieves a specific instructor by ID
        /// </summary>
        /// <param name="id">The ID of the instructor to retrieve</param>
        /// <returns>The instructor with the specified ID, or null if not found</returns>
        public async Task<Instructor?> GetInstructorByIdAsync(int id)
        {
            return await _instructorRepository.GetInstructorByIdAsync(id);
        }

        /// <summary>
        /// Creates a new instructor record
        /// </summary>
        /// <param name="createInstructor">The instructor data to create</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A tuple containing the created instructor (if successful) and an error message (if any)</returns>
        public async Task<(Instructor?, string?)> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal userPrincipal)
        {
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

        /// <summary>
        /// Updates an existing instructor record
        /// </summary>
        /// <param name="id">The ID of the instructor to update</param>
        /// <param name="updateInstructor">The updated instructor data</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A tuple containing the updated instructor (if successful) and an error message (if any)</returns>
        public async Task<(Instructor?, string?)> UpdateInstructorAsync(int id, UpdateInstructor updateInstructor, ClaimsPrincipal userPrincipal)
        {
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

        /// <summary>
        /// Soft deletes an instructor record
        /// </summary>
        /// <param name="id">The ID of the instructor to delete</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A message indicating the result of the operation</returns>
        public async Task<string?> SoftDeleteInstructorAsync(int id, ClaimsPrincipal userPrincipal)
        {
            if (id <= 0)
            {
                return "Invalid instructor ID";
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                return "User ID not found in token";
            }

            var existingInstructor = await _instructorRepository.GetInstructorByIdAsync(id);
            if (existingInstructor == null)
            {
                return "Instructor not found";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingInstructor.UserId, "Admin", "Teacher");
            if (!isAuthorized)
            {
                return "You are not authorized to delete this instructor record.";
            }

            var result = await _instructorRepository.SoftDeleteInstructor(id);
            return !result ? "Failed to soft delete instructor" : null;
        }

        /// <summary>
        /// Hard deletes an instructor record
        /// </summary>
        /// <param name="id">The ID of the instructor to delete</param>
        /// <param name="userPrincipal">The claims principal of the current user</param>
        /// <returns>A message indicating the result of the operation</returns>
        public async Task<string?> HardDeleteInstructorAsync(int id, ClaimsPrincipal userPrincipal)
        {
            if (id <= 0)
            {
                return "Invalid instructor ID";
            }

            var userId = await _userContextService.GetUserIdAsync(userPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                return "User ID not found in token";
            }

            var existingInstructor = await _instructorRepository.GetInstructorByIdAsync(id);
            if (existingInstructor == null)
            {
                return "Instructor not found";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(userPrincipal, existingInstructor.UserId, "Admin");
            if (!isAuthorized)
            {
                return "You are not authorized to permanently delete this instructor record.";
            }

            var result = await _instructorRepository.HardDeleteInstructor(id);
            return !result ? "Failed to hard delete instructor" : null;
        }
    }
}
