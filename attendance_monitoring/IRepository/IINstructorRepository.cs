using attendance_monitoring.Classes;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.IRepository;

public interface IInstructorRepository
{
    Task<IEnumerable<Instructor>> GetAllInstructorsAsync(PaginationQuery paginationQuery);
    Task<Instructor?> GetInstructorByIdAsync(int id);
    Task<Instructor?> GetInstructorByUserIdAsync(string userId);
    Task<Instructor> CreateInstructor(Instructor instructor);
    Task<Instructor> UpdateInstructorAsync(Instructor instructor);
    Task<int> SaveChangesAsync();
}