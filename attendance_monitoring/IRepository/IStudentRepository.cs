using attendance_monitoring.Classes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace attendance_monitoring.Repositories;

public interface IStudentRepository
{
    Task<IEnumerable<Student>> GetAllStudentsAsync();
    Task<Student?> GetStudentByIdAsync(string id);
}