using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.IServices;

public interface IInstructorService
{
    Task<IEnumerable<Instructor>> GetAllInstructorsAsync(PaginationQuery paginationQuery);
    Task<Instructor?> GetInstructorByIdAsync(int id);
    Task<(Instructor, string)> CreateInstructorAsync(CreateInstructor createInstructor, ClaimsPrincipal user);
    Task<(Instructor, string)> UpdateInstructorAsync(int id, UpdateInstructor updateInstructor, ClaimsPrincipal user);
}