using attendance_monitoring.Classes;
using System.Collections.Generic;
using System.Threading.Tasks;
using attendance_monitoring.Models.Request;

namespace attendance_monitoring.Repositories;

public interface IStudentRepository
{
    Task<IEnumerable<Student>> GetAllStudentsAsync(PaginationQuery paginationQuery);
    Task<Student?> GetStudentByIdAsync(int id);
    Task<Student?> GetStudentByUserIdAsync(string userId);
    Task<Student> CreateStudent(Student student);
    Task<Student> UpdateStudentAsync(Student student);
    Task<int> SaveChangesAsync();
}