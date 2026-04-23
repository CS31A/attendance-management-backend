using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class InstructorControllerTest
{
    private readonly Mock<IInstructorService> _mockInstructorService;
    private readonly Mock<ILogger<InstructorController>> _mockLogger;
    private readonly InstructorController _instructorController;

    public InstructorControllerTest()
    {
        _mockInstructorService = new Mock<IInstructorService>();
        _mockLogger = new Mock<ILogger<InstructorController>>();
        _instructorController = new InstructorController(_mockInstructorService.Object, _mockLogger.Object);

        var mockUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "mock"));

        _instructorController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = mockUser }
        };
    }

    #region GetInstructors Tests

    [Fact]
    public async Task GetInstructors_ReturnsOkResult_WithInstructorsList()
    {
        // Arrange
        var expectedInstructors = new List<Instructor>
        {
            new Instructor { Id = 1, Firstname = "John", Lastname = "Doe" },
            new Instructor { Id = 2, Firstname = "Jane", Lastname = "Smith" }
        };
        _mockInstructorService
            .Setup(s => s.GetAllInstructorsAsync())
            .ReturnsAsync(expectedInstructors);

        // Act
        var result = await _instructorController.GetInstructors();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var instructors = Assert.IsAssignableFrom<IEnumerable<Instructor>>(okResult.Value);
        Assert.Equal(2, instructors.Count());
        _mockInstructorService.Verify(s => s.GetAllInstructorsAsync(), Times.Once);
    }

    #endregion

    #region GetMySchedules Tests

    [Fact]
    public async Task GetMySchedules_ReturnsOkResult_WithSchedulesList()
    {
        // Arrange
        var expectedSchedules = new List<ScheduleResponseDto>
        {
            new ScheduleResponseDto
            {
                Id = 1,
                TimeIn = new TimeOnly(8, 0),
                TimeOut = new TimeOnly(10, 0),
                DayOfWeek = "Monday",
                Subject = new SubjectResponseDto { Id = 1, Name = "Math", Code = "MATH101" },
                Classroom = new ClassroomResponseDto { Id = 1, Name = "Room 101" },
                Section = new SectionResponseDto { Id = 1, Name = "Section A", CourseId = 1 },
                Instructor = new InstructorResponseDto { Id = 1, Firstname = "John", Lastname = "Doe" }
            },
            new ScheduleResponseDto
            {
                Id = 2,
                TimeIn = new TimeOnly(10, 0),
                TimeOut = new TimeOnly(12, 0),
                DayOfWeek = "Tuesday",
                Subject = new SubjectResponseDto { Id = 2, Name = "Science", Code = "SCI101" },
                Classroom = new ClassroomResponseDto { Id = 2, Name = "Room 102" },
                Section = new SectionResponseDto { Id = 2, Name = "Section B", CourseId = 1 },
                Instructor = new InstructorResponseDto { Id = 1, Firstname = "John", Lastname = "Doe" }
            }
        };
        _mockInstructorService
            .Setup(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(expectedSchedules);

        // Act
        var result = await _instructorController.GetMySchedules();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var schedules = Assert.IsAssignableFrom<IEnumerable<ScheduleResponseDto>>(okResult.Value);
        Assert.Equal(2, schedules.Count());
        _mockInstructorService.Verify(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySchedules_ReturnsOkResult_WithEmptyList_WhenNoSchedules()
    {
        // Arrange
        var expectedSchedules = new List<ScheduleResponseDto>();
        _mockInstructorService
            .Setup(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(expectedSchedules);

        // Act
        var result = await _instructorController.GetMySchedules();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var schedules = Assert.IsAssignableFrom<IEnumerable<ScheduleResponseDto>>(okResult.Value);
        Assert.Empty(schedules);
        _mockInstructorService.Verify(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySchedules_ReturnsNotFound_WhenInstructorNotFound()
    {
        // Arrange
        _mockInstructorService
            .Setup(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityNotFoundException<string>("Instructor", "UserId: 1"));

        // Act
        var result = await _instructorController.GetMySchedules();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("No instructor record found for the current user", notFoundResult.Value);
        _mockInstructorService.Verify(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySchedules_ReturnsInternalServerError_WhenServiceExceptionOccurs()
    {
        // Arrange
        _mockInstructorService
            .Setup(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityServiceException("Instructor", "GetSchedulesByInstructor", "Service error"));

        // Act
        var result = await _instructorController.GetMySchedules();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while retrieving the schedules", statusCodeResult.Value);
        _mockInstructorService.Verify(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    #endregion

    #region GetMySectionsWithStudents Tests

    [Fact]
    public async Task GetMySectionsWithStudents_ReturnsOkResult_WithSectionsAndStudents()
    {
        // Arrange
        var expectedResponse = new InstructorSectionsWithStudentsResponseDto
        {
            InstructorId = 1,
            InstructorUuid = Guid.NewGuid(),
            InstructorFirstname = "John",
            InstructorLastname = "Doe",
            Sections = new List<SectionWithStudentsDto>
            {
                new SectionWithStudentsDto
                {
                    SectionId = 1,
                    SectionName = "BSCS 3A",
                    CourseId = 1,
                    CourseName = "Bachelor of Science in Computer Science",
                    Subjects = new List<SubjectScheduleDto>
                    {
                        new SubjectScheduleDto
                        {
                            SubjectId = 1,
                            SubjectName = "Data Structures",
                            SubjectCode = "CS301",
                            ScheduleId = 1,
                            DayOfWeek = "Monday",
                            TimeIn = new TimeOnly(8, 0),
                            TimeOut = new TimeOnly(10, 0),
                            ClassroomName = "Room 101",
                            Students = new List<StudentDto>
                            {
                                new StudentDto
                                {
                                    StudentId = 1,
                                    Firstname = "Alice",
                                    Lastname = "Smith",
                                    IsRegular = true,
                                    EnrollmentType = "Regular"
                                }
                            }
                        }
                    }
                }
            }
        };
        _mockInstructorService
            .Setup(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _instructorController.GetMySectionsWithStudents();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InstructorSectionsWithStudentsResponseDto>(okResult.Value);
        Assert.Equal(1, response.InstructorId);
        Assert.Equal("John", response.InstructorFirstname);
        Assert.Equal("Doe", response.InstructorLastname);
        Assert.Single(response.Sections);
        _mockInstructorService.Verify(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySectionsWithStudents_ReturnsNotFound_WhenInstructorNotFound()
    {
        // Arrange
        _mockInstructorService
            .Setup(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityNotFoundException<string>("Instructor", "UserId: 1"));

        // Act
        var result = await _instructorController.GetMySectionsWithStudents();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("No instructor record found for the current user", notFoundResult.Value);
        _mockInstructorService.Verify(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySectionsWithStudents_ReturnsInternalServerError_WhenServiceExceptionOccurs()
    {
        // Arrange
        _mockInstructorService
            .Setup(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityServiceException("Instructor", "GetSectionsWithStudentsByInstructor", "Service error"));

        // Act
        var result = await _instructorController.GetMySectionsWithStudents();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while retrieving sections", statusCodeResult.Value);
        _mockInstructorService.Verify(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    #endregion
}