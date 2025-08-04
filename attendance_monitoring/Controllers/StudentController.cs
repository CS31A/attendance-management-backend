using attendance_monitoring.Classes;
using attendance_monitoring.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentController : ControllerBase
{

    private IStudentRepository student;

    public StudentController(IStudentRepository student)
    {
        this.student = student;
    }
}