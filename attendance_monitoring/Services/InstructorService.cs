using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.Request;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.Services
{
    public class InstructorService : IInstructorService
    {
        private readonly IInstructorRepository _instructorRepository;
        private readonly UserManager<IdentityUser> _userManager;

        public InstructorService(IInstructorRepository instructorRepository, UserManager<IdentityUser> userManager)
        {
            _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<IEnumerable<Instructor>> GetAllInstructorsAsync(PaginationQuery paginationQuery)
        {
            return await _instructorRepository.GetAllInstructorsAsync(paginationQuery);
        }

        public async Task<Instructor> GetInstructorByIdAsync(int id)
        {
            return await _instructorRepository.GetInstructorByIdAsync(id);
        }

        public async Task<(Instructor, string)> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal userPrincipal)
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

            var userId = await GetUserIdAsync(userPrincipal);
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

        public async Task<(Instructor, string)> UpdateInstructorAsync(int id, UpdateInstructor updateInstructor, ClaimsPrincipal userPrincipal)
        {
            // Additional validation for defense in depth
            if (updateInstructor == null)
            {
                return (null, "Update instructor data is required");
            }

            var userId = await GetUserIdAsync(userPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                return (null, "User ID not found in token");
            }

            var existingInstructor = await _instructorRepository.GetInstructorByIdAsync(id);
            if (existingInstructor == null)
            {
                return (null, "Instructor not found");
            }

            var userRole = userPrincipal.FindFirst(ClaimTypes.Role)?.Value;
            var isAuthorized = existingInstructor.UserId == userId ||
                               (userRole != null && (userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                                    userRole.Equals("Teacher", StringComparison.OrdinalIgnoreCase)));

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

        private async Task<string> GetUserIdAsync(ClaimsPrincipal userPrincipal)
        {
            // Null check for defense in depth
            if (userPrincipal == null)
            {
                return null;
            }

            var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId) && userId == userPrincipal.FindFirst(ClaimTypes.Name)?.Value)
            {
                var username = userId;
                // Null check for _userManager
                if (_userManager != null)
                {
                    var user = await _userManager.FindByNameAsync(username);
                    if (user != null)
                    {
                        return user.Id;
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(userId))
            {
                return userId;
            }

            userId = userPrincipal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return userId;
            }

            userId = userPrincipal.FindFirst(ClaimTypes.Name)?.Value;
            return userId;
        }
    }
}