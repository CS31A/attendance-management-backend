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
    private readonly Mock<IInstructorCrudService> _mockInstructorCrudService;
    private readonly Mock<IInstructorQueryService> _mockInstructorQueryService;
    private readonly Mock<IInstructorDetailService> _mockInstructorDetailService;
    private readonly Mock<ILogger<InstructorController>> _mockLogger;
    private readonly InstructorController _instructorController;

    public InstructorControllerTest()
    {
        _mockInstructorCrudService = new Mock<IInstructorCrudService>();
        _mockInstructorQueryService = new Mock<IInstructorQueryService>();
        _mockInstructorDetailService = new Mock<IInstructorDetailService>();
        _mockLogger = new Mock<ILogger<InstructorController>>();
        _instructorController = new InstructorController(_mockInstructorCrudService.Object, _mockInstructorQueryService.Object, _mockInstructorDetailService.Object, _mockLogger.Object);

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
            new Instructor { Id = Guid.NewGuid(), Firstname = "John", Lastname = "Doe" },
            new Instructor { Id = Guid.NewGuid(), Firstname = "Jane", Lastname = "Smith" }
        };
        _mockInstructorCrudService
            .Setup(s => s.GetAllInstructorsAsync())
            .ReturnsAsync(expectedInstructors);

        // Act
        var result = await _instructorController.GetInstructors();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var instructors = Assert.IsAssignableFrom<IEnumerable<Instructor>>(okResult.Value);
        Assert.Equal(2, instructors.Count());
        _mockInstructorCrudService.Verify(s => s.GetAllInstructorsAsync(), Times.Once);
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
                Id = Guid.NewGuid(),
                TimeIn = new TimeOnly(8, 0),
                TimeOut = new TimeOnly(10, 0),
                DayOfWeek = "Monday",
                Subject = new SubjectResponseDto { Id = Guid.NewGuid(), Name = "Math", Code = "MATH101" },
                Classroom = new ClassroomResponseDto { Id = Guid.NewGuid(), Name = "Room 101" },
                Section = new SectionResponseDto { Id = Guid.NewGuid(), Name = "Section A", CourseId = Guid.NewGuid() },
                Instructor = new InstructorResponseDto { Id = Guid.NewGuid(), Firstname = "John", Lastname = "Doe" }
            },
            new ScheduleResponseDto
            {
                Id = Guid.NewGuid(),
                TimeIn = new TimeOnly(10, 0),
                TimeOut = new TimeOnly(12, 0),
                DayOfWeek = "Tuesday",
                Subject = new SubjectResponseDto { Id = Guid.NewGuid(), Name = "Science", Code = "SCI101" },
                Classroom = new ClassroomResponseDto { Id = Guid.NewGuid(), Name = "Room 102" },
                Section = new SectionResponseDto { Id = Guid.NewGuid(), Name = "Section B", CourseId = Guid.NewGuid() },
                Instructor = new InstructorResponseDto { Id = Guid.NewGuid(), Firstname = "John", Lastname = "Doe" }
            }
        };
        _mockInstructorQueryService
            .Setup(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(expectedSchedules);

        // Act
        var result = await _instructorController.GetMySchedules();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var schedules = Assert.IsAssignableFrom<IEnumerable<ScheduleResponseDto>>(okResult.Value);
        Assert.Equal(2, schedules.Count());
        _mockInstructorQueryService.Verify(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySchedules_ReturnsOkResult_WithEmptyList_WhenNoSchedules()
    {
        // Arrange
        var expectedSchedules = new List<ScheduleResponseDto>();
        _mockInstructorQueryService
            .Setup(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(expectedSchedules);

        // Act
        var result = await _instructorController.GetMySchedules();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var schedules = Assert.IsAssignableFrom<IEnumerable<ScheduleResponseDto>>(okResult.Value);
        Assert.Empty(schedules);
        _mockInstructorQueryService.Verify(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySchedules_ThrowsEntityNotFoundException_WhenInstructorNotFound()
    {
        // Arrange
        _mockInstructorQueryService
            .Setup(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityNotFoundException<string>("Instructor", "UserId: 1"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityNotFoundException<string>>(
            () => _instructorController.GetMySchedules());
        _mockInstructorQueryService.Verify(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySchedules_ThrowsEntityServiceException_WhenServiceExceptionOccurs()
    {
        // Arrange
        _mockInstructorQueryService
            .Setup(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityServiceException("Instructor", "GetSchedulesByInstructor", "Service error"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityServiceException>(
            () => _instructorController.GetMySchedules());
        _mockInstructorQueryService.Verify(s => s.GetSchedulesByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    #endregion

    #region GetMySectionsWithStudents Tests

    [Fact]
    public async Task GetMySectionsWithStudents_ReturnsOkResult_WithSectionsAndStudents()
    {
        // Arrange
        var expectedResponse = new InstructorSectionsWithStudentsResponseDto
        {
            InstructorId = Guid.NewGuid(),
            InstructorFirstname = "John",
            InstructorLastname = "Doe",
            Sections = new List<SectionWithStudentsDto>
            {
                new SectionWithStudentsDto
                {
                    SectionId = Guid.NewGuid(),
                    SectionName = "BSCS 3A",
                    CourseId = Guid.NewGuid(),
                    CourseName = "Bachelor of Science in Computer Science",
                    Subjects = new List<SubjectScheduleDto>
                    {
                        new SubjectScheduleDto
                        {
                            SubjectId = Guid.NewGuid(),
                            SubjectName = "Data Structures",
                            SubjectCode = "CS301",
                            ScheduleId = Guid.NewGuid(),
                            DayOfWeek = "Monday",
                            TimeIn = new TimeOnly(8, 0),
                            TimeOut = new TimeOnly(10, 0),
                            ClassroomId = Guid.NewGuid(),
                            ClassroomName = "Room 101",
                            Students = new List<StudentDto>
                            {
                                new StudentDto
                                {
                                    StudentId = Guid.NewGuid(),
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
        _mockInstructorQueryService
            .Setup(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _instructorController.GetMySectionsWithStudents();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<InstructorSectionsWithStudentsResponseDto>(okResult.Value);
        Assert.Equal(expectedResponse.InstructorId, response.InstructorId);
        Assert.Equal("John", response.InstructorFirstname);
        Assert.Equal("Doe", response.InstructorLastname);
        Assert.Single(response.Sections);
        _mockInstructorQueryService.Verify(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySectionsWithStudents_ThrowsEntityNotFoundException_WhenInstructorNotFound()
    {
        // Arrange
        _mockInstructorQueryService
            .Setup(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityNotFoundException<string>("Instructor", "UserId: 1"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityNotFoundException<string>>(
            () => _instructorController.GetMySectionsWithStudents());
        _mockInstructorQueryService.Verify(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySectionsWithStudents_ThrowsEntityServiceException_WhenServiceExceptionOccurs()
    {
        // Arrange
        _mockInstructorQueryService
            .Setup(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityServiceException("Instructor", "GetSectionsWithStudentsByInstructor", "Service error"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityServiceException>(
            () => _instructorController.GetMySectionsWithStudents());
        _mockInstructorQueryService.Verify(s => s.GetSectionsWithStudentsByInstructorAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    #endregion

    #region GetMySectionsOverview Tests

    [Fact]
    public async Task GetMySectionsOverview_ReturnsOkResult_WithSectionsOverviewList()
    {
        // Arrange
        var expectedSections = new List<InstructorSectionOverviewDto>
        {
            new InstructorSectionOverviewDto
            {
                SectionId = Guid.NewGuid(),
                SectionName = "BSCS 3A",
                CourseId = Guid.NewGuid(),
                CourseName = "Computer Science",
                HandledClassCount = 2,
                UniqueStudentCount = 30
            },
            new InstructorSectionOverviewDto
            {
                SectionId = Guid.NewGuid(),
                SectionName = "BSCS 3B",
                CourseId = Guid.NewGuid(),
                CourseName = "Computer Science",
                HandledClassCount = 1,
                UniqueStudentCount = 25
            }
        };
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorSectionsOverviewAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(expectedSections);

        // Act
        var result = await _instructorController.GetMySectionsOverview();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sections = Assert.IsAssignableFrom<List<InstructorSectionOverviewDto>>(okResult.Value);
        Assert.Equal(2, sections.Count);
        _mockInstructorDetailService.Verify(s => s.GetInstructorSectionsOverviewAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySectionsOverview_ThrowsEntityNotFoundException_WhenInstructorNotFound()
    {
        // Arrange
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorSectionsOverviewAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityNotFoundException<string>("Instructor", "UserId: 1"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityNotFoundException<string>>(
            () => _instructorController.GetMySectionsOverview());
        _mockInstructorDetailService.Verify(s => s.GetInstructorSectionsOverviewAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task GetMySectionsOverview_ThrowsEntityServiceException_WhenServiceExceptionOccurs()
    {
        // Arrange
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorSectionsOverviewAsync(It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityServiceException("Instructor", "GetInstructorSectionsOverview", "Service error"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityServiceException>(
            () => _instructorController.GetMySectionsOverview());
        _mockInstructorDetailService.Verify(s => s.GetInstructorSectionsOverviewAsync(It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    #endregion

    #region GetMySectionDetail Tests

    [Fact]
    public async Task GetMySectionDetail_ReturnsOkResult_WithSectionDetail()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        var expectedDetail = new InstructorSectionDetailDto
        {
            SectionId = sectionId,
            SectionName = "BSCS 3A",
            CourseId = Guid.NewGuid(),
            CourseName = "Computer Science",
            HandledClassCount = 2,
            HomeSectionStudentCount = 30,
            HandledClasses = new List<InstructorHandledClassDto>
            {
                new InstructorHandledClassDto
                {
                    SubjectId = Guid.NewGuid(),
                    SubjectName = "Data Structures",
                    SubjectCode = "CS301",
                    ScheduleId = Guid.NewGuid(),
                    DayOfWeek = "Monday",
                    TimeIn = new TimeOnly(8, 0),
                    TimeOut = new TimeOnly(10, 0),
                    ClassroomId = Guid.NewGuid(),
                    ClassroomName = "Room 101",
                    StudentCount = 30,
                    Students = new List<InstructorHandledClassStudentDto>()
                }
            },
            HomeSectionStudents = new List<InstructorHomeSectionStudentDto>()
        };
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorSectionDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), sectionId))
            .ReturnsAsync(expectedDetail);

        // Act
        var result = await _instructorController.GetMySectionDetail(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var detail = Assert.IsType<InstructorSectionDetailDto>(okResult.Value);
        Assert.Equal(expectedDetail.SectionId, detail.SectionId);
        Assert.Equal("BSCS 3A", detail.SectionName);
        _mockInstructorDetailService.Verify(s => s.GetInstructorSectionDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), sectionId), Times.Once);
    }

    [Fact]
    public async Task GetMySectionDetail_ThrowsEntityNotFoundException_WhenInstructorNotFound()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorSectionDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), sectionId))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityNotFoundException<string>("Instructor", "UserId: 1"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityNotFoundException<string>>(
            () => _instructorController.GetMySectionDetail(sectionId));
    }

    [Fact]
    public async Task GetMySectionDetail_ThrowsEntityNotFoundException_WhenSectionNotFound()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorSectionDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), sectionId))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityNotFoundException<Guid>("Section", sectionId));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityNotFoundException<Guid>>(
            () => _instructorController.GetMySectionDetail(sectionId));
    }

    [Fact]
    public async Task GetMySectionDetail_ThrowsEntityUnauthorizedException_WhenNotAuthorized()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorSectionDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), sectionId))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityUnauthorizedException("Section", "View section", "1", "You are not authorized to view this section"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityUnauthorizedException>(
            () => _instructorController.GetMySectionDetail(sectionId));
    }

    [Fact]
    public async Task GetMySectionDetail_ThrowsEntityServiceException_WhenServiceExceptionOccurs()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorSectionDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), sectionId))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityServiceException("Instructor", "GetInstructorSectionDetail", "Service error"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityServiceException>(
            () => _instructorController.GetMySectionDetail(sectionId));
    }

    #endregion

    #region GetMyStudentDetail Tests

    [Fact]
    public async Task GetMyStudentDetail_ReturnsOkResult_WithStudentDetail()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var expectedDetail = new InstructorStudentDetailDto
        {
            StudentId = studentId,
            Firstname = "Alice",
            Lastname = "Smith",
            SectionId = Guid.NewGuid(),
            SectionName = "BSCS 3A",
            CourseId = Guid.NewGuid(),
            CourseName = "Computer Science",
            IsRegular = true,
            EnrollmentType = "Regular",
            Enrollments = new List<InstructorStudentEnrollmentDto>
            {
                new InstructorStudentEnrollmentDto
                {
                    SubjectId = Guid.NewGuid(),
                    SubjectName = "Data Structures",
                    SubjectCode = "CS301",
                    SectionId = Guid.NewGuid(),
                    SectionName = "BSCS 3A",
                    EnrollmentType = "Regular"
                }
            },
            AttendanceSummary = new InstructorStudentAttendanceSummaryDto
            {
                TotalSessions = 10,
                PresentCount = 8,
                AbsentCount = 1,
                LateCount = 1,
                AttendanceRate = 90.0
            }
        };
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorStudentDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), studentId))
            .ReturnsAsync(expectedDetail);

        // Act
        var result = await _instructorController.GetMyStudentDetail(studentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var detail = Assert.IsType<InstructorStudentDetailDto>(okResult.Value);
        Assert.Equal(expectedDetail.StudentId, detail.StudentId);
        Assert.Equal("Alice", detail.Firstname);
        Assert.Equal("Smith", detail.Lastname);
        _mockInstructorDetailService.Verify(s => s.GetInstructorStudentDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), studentId), Times.Once);
    }

    [Fact]
    public async Task GetMyStudentDetail_ThrowsEntityNotFoundException_WhenInstructorNotFound()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorStudentDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), studentId))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityNotFoundException<string>("Instructor", "UserId: 1"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityNotFoundException<string>>(
            () => _instructorController.GetMyStudentDetail(studentId));
    }

    [Fact]
    public async Task GetMyStudentDetail_ThrowsEntityNotFoundException_WhenStudentNotFound()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorStudentDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), studentId))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityNotFoundException<Guid>("Student", studentId));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityNotFoundException<Guid>>(
            () => _instructorController.GetMyStudentDetail(studentId));
    }

    [Fact]
    public async Task GetMyStudentDetail_ThrowsEntityUnauthorizedException_WhenNotAuthorized()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorStudentDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), studentId))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityUnauthorizedException("Student", "View student", "1", "You are not authorized to view this student"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityUnauthorizedException>(
            () => _instructorController.GetMyStudentDetail(studentId));
    }

    [Fact]
    public async Task GetMyStudentDetail_ThrowsEntityServiceException_WhenServiceExceptionOccurs()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockInstructorDetailService
            .Setup(s => s.GetInstructorStudentDetailByUuidAsync(It.IsAny<ClaimsPrincipal>(), studentId))
            .ThrowsAsync(new attendance_monitoring.Exceptions.EntityServiceException("Instructor", "GetInstructorStudentDetail", "Service error"));

        // Act & Assert
        await Assert.ThrowsAsync<attendance_monitoring.Exceptions.EntityServiceException>(
            () => _instructorController.GetMyStudentDetail(studentId));
    }

    #endregion
}
