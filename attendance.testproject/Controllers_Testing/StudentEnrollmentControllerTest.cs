using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Exceptions;

namespace attendance.testproject.Controllers_Testing;

/// <summary>
/// Unit tests for StudentEnrollmentController
/// Tests all 6 API endpoints with various scenarios
/// </summary>
public class StudentEnrollmentControllerTest
{
    private readonly Mock<IStudentEnrollmentService> _mockService;
    private readonly Mock<ILogger<StudentEnrollmentController>> _mockLogger;
    private readonly StudentEnrollmentController _controller;

    public StudentEnrollmentControllerTest()
    {
        _mockService = new Mock<IStudentEnrollmentService>();
        _mockLogger = new Mock<ILogger<StudentEnrollmentController>>();
        _controller = new StudentEnrollmentController(_mockService.Object, _mockLogger.Object);
    }

    #region EnrollStudent Tests

    [Fact]
    public async Task EnrollStudent_ValidRequest_ReturnsOkWithEnrollmentResponse()
    {
        // Arrange
        var request = new CreateStudentEnrollment
        {
            StudentId = 1,
            SectionId = 2,
            SubjectId = 3,
            EnrollmentType = "Retake",
            AcademicYear = "2024-2025",
            Semester = "First"
        };

        var enrollment = new StudentEnrollment
        {
            Id = 10,
            StudentId = 1,
            SectionId = 2,
            SubjectId = 3,
            IsActive = true,
            EnrollmentType = "Retake",
            AcademicYear = "2024-2025",
            Semester = "First",
            EnrolledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Student = new Student { Id = 1, Firstname = "John", Lastname = "Doe", Email = "john@test.com" },
            Section = new Section { Id = 2, Name = "CS-3B" },
            Subject = new Subject { Id = 3, Name = "Database Systems", Code = "CS301" }
        };

        _mockService.Setup(s => s.EnrollStudentAsync(
            request.StudentId,
            request.SectionId,
            request.SubjectId,
            request.EnrollmentType,
            request.AcademicYear,
            request.Semester))
            .ReturnsAsync(enrollment);

        // Act
        var result = await _controller.EnrollStudent(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudentEnrollmentResponseDto>(okResult.Value);
        Assert.Equal(10, response.Id);
        Assert.Equal(1, response.StudentId);
        Assert.Equal("John", response.StudentFirstname);
        Assert.Equal("CS-3B", response.SectionName);
        Assert.Equal("Database Systems", response.SubjectName);
        Assert.Equal("Retake", response.EnrollmentType);
        Assert.True(response.IsActive);
        _mockService.Verify(s => s.EnrollStudentAsync(
            request.StudentId,
            request.SectionId,
            request.SubjectId,
            request.EnrollmentType,
            request.AcademicYear,
            request.Semester), Times.Once);
    }

    [Fact]
    public async Task EnrollStudent_StudentNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateStudentEnrollment
        {
            StudentId = 999,
            SectionId = 2,
            SubjectId = 3
        };

        _mockService.Setup(s => s.EnrollStudentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new EntityNotFoundException<int>("Student", 999));

        // Act
        var result = await _controller.EnrollStudent(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task EnrollStudent_DuplicateEnrollment_ReturnsConflict()
    {
        // Arrange
        var request = new CreateStudentEnrollment
        {
            StudentId = 1,
            SectionId = 2,
            SubjectId = 3
        };

        _mockService.Setup(s => s.EnrollStudentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new EntityAlreadyExistsException<string>("Enrollment", "Combination", "Already enrolled"));

        // Act
        var result = await _controller.EnrollStudent(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task EnrollStudent_DuplicateEnrollmentIntException_ReturnsConflict()
    {
        // Arrange
        var request = new CreateStudentEnrollment
        {
            StudentId = 1,
            SectionId = 2,
            SubjectId = 3
        };

        _mockService.Setup(s => s.EnrollStudentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new EntityAlreadyExistsException<int>("Enrollment", "Id", 1));

        // Act
        var result = await _controller.EnrollStudent(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.NotNull(conflictResult.Value);
    }

    #endregion

    #region GetStudentEnrollments Tests

    [Fact]
    public async Task GetStudentEnrollments_ValidStudentId_ReturnsOkWithEnrollments()
    {
        // Arrange
        var studentId = 1;
        var enrollments = new List<StudentEnrollment>
        {
            new StudentEnrollment
            {
                Id = 1,
                StudentId = studentId,
                SectionId = 2,
                SubjectId = 3,
                IsActive = true,
                EnrollmentType = "Retake",
                EnrolledAt = DateTime.UtcNow,
                Section = new Section { Id = 2, Name = "CS-3B" },
                Subject = new Subject { Id = 3, Name = "Database Systems", Code = "CS301" }
            },
            new StudentEnrollment
            {
                Id = 2,
                StudentId = studentId,
                SectionId = 3,
                SubjectId = 4,
                IsActive = false,
                EnrollmentType = "Irregular",
                EnrolledAt = DateTime.UtcNow,
                Section = new Section { Id = 3, Name = "CS-4A" },
                Subject = new Subject { Id = 4, Name = "Machine Learning", Code = "CS402" }
            }
        };

        _mockService.Setup(s => s.GetStudentEnrollmentsAsync(studentId))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _controller.GetStudentEnrollments(studentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudentSectionsResponseDto>(okResult.Value);
        Assert.Equal(studentId, response.StudentId);
        Assert.Equal(2, response.Enrollments.Count);
        Assert.Equal("CS-3B", response.Enrollments[0].SectionName);
        Assert.Equal("Database Systems", response.Enrollments[0].SubjectName);
        Assert.True(response.Enrollments[0].IsActive);
        Assert.False(response.Enrollments[1].IsActive);
        _mockService.Verify(s => s.GetStudentEnrollmentsAsync(studentId), Times.Once);
    }

    [Fact]
    public async Task GetStudentEnrollments_NoEnrollments_ReturnsOkWithEmptyList()
    {
        // Arrange
        var studentId = 1;
        var enrollments = new List<StudentEnrollment>();

        _mockService.Setup(s => s.GetStudentEnrollmentsAsync(studentId))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _controller.GetStudentEnrollments(studentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudentSectionsResponseDto>(okResult.Value);
        Assert.Equal(studentId, response.StudentId);
        Assert.Empty(response.Enrollments);
    }

    #endregion

    #region GetSectionStudents Tests

    [Fact]
    public async Task GetSectionStudents_ValidSectionId_ReturnsOkWithStudents()
    {
        // Arrange
        var sectionId = 1;
        var enrollments = new List<StudentEnrollment>
        {
            new StudentEnrollment
            {
                Id = 1,
                StudentId = 1,
                SectionId = sectionId,
                SubjectId = 3,
                IsActive = true,
                EnrollmentType = "Retake",
                EnrolledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Student = new Student { Id = 1, Firstname = "John", Lastname = "Doe", Email = "john@test.com" },
                Section = new Section { Id = sectionId, Name = "CS-3B" },
                Subject = new Subject { Id = 3, Name = "Database Systems", Code = "CS301" }
            },
            new StudentEnrollment
            {
                Id = 2,
                StudentId = 2,
                SectionId = sectionId,
                SubjectId = 4,
                IsActive = true,
                EnrollmentType = "Irregular",
                EnrolledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Student = new Student { Id = 2, Firstname = "Jane", Lastname = "Smith", Email = "jane@test.com" },
                Section = new Section { Id = sectionId, Name = "CS-3B" },
                Subject = new Subject { Id = 4, Name = "Web Development", Code = "CS302" }
            }
        };

        _mockService.Setup(s => s.GetActiveSectionEnrollmentsAsync(sectionId))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _controller.GetSectionStudents(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IEnumerable<StudentEnrollmentResponseDto>>(okResult.Value);
        var responseList = response.ToList();
        Assert.Equal(2, responseList.Count);
        Assert.Equal("John", responseList[0].StudentFirstname);
        Assert.Equal("Jane", responseList[1].StudentFirstname);
        Assert.All(responseList, r => Assert.True(r.IsActive));
        _mockService.Verify(s => s.GetActiveSectionEnrollmentsAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task GetSectionStudents_NoEnrollments_ReturnsOkWithEmptyList()
    {
        // Arrange
        var sectionId = 1;
        var enrollments = new List<StudentEnrollment>();

        _mockService.Setup(s => s.GetActiveSectionEnrollmentsAsync(sectionId))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _controller.GetSectionStudents(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IEnumerable<StudentEnrollmentResponseDto>>(okResult.Value);
        Assert.Empty(response);
    }

    #endregion

    #region DropStudent Tests

    [Fact]
    public async Task DropStudent_ValidEnrollmentId_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        var enrollmentId = 10;
        _mockService.Setup(s => s.DropStudentFromSubjectAsync(enrollmentId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DropStudent(enrollmentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockService.Verify(s => s.DropStudentFromSubjectAsync(enrollmentId), Times.Once);
    }

    [Fact]
    public async Task DropStudent_EnrollmentNotFound_ReturnsNotFound()
    {
        // Arrange
        var enrollmentId = 999;
        _mockService.Setup(s => s.DropStudentFromSubjectAsync(enrollmentId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DropStudent(enrollmentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task DropStudent_ServiceThrowsEntityNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var enrollmentId = 999;
        _mockService.Setup(s => s.DropStudentFromSubjectAsync(enrollmentId))
            .ThrowsAsync(new EntityNotFoundException<int>("Enrollment", enrollmentId));

        // Act
        var result = await _controller.DropStudent(enrollmentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region ReenrollStudent Tests

    [Fact]
    public async Task ReenrollStudent_ValidEnrollmentId_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        var enrollmentId = 10;
        _mockService.Setup(s => s.ReenrollStudentAsync(enrollmentId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReenrollStudent(enrollmentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockService.Verify(s => s.ReenrollStudentAsync(enrollmentId), Times.Once);
    }

    [Fact]
    public async Task ReenrollStudent_EnrollmentNotFound_ReturnsNotFound()
    {
        // Arrange
        var enrollmentId = 999;
        _mockService.Setup(s => s.ReenrollStudentAsync(enrollmentId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ReenrollStudent(enrollmentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task ReenrollStudent_ServiceThrowsEntityNotFoundException_ReturnsNotFound()
    {
        // Arrange
        var enrollmentId = 999;
        _mockService.Setup(s => s.ReenrollStudentAsync(enrollmentId))
            .ThrowsAsync(new EntityNotFoundException<int>("Enrollment", enrollmentId));

        // Act
        var result = await _controller.ReenrollStudent(enrollmentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    #endregion

    #region CheckEnrollment Tests

    [Fact]
    public async Task CheckEnrollment_StudentIsEnrolled_ReturnsOkWithTrue()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        _mockService.Setup(s => s.IsStudentEnrolledInSectionSubjectAsync(studentId, sectionId, subjectId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckEnrollment(studentId, sectionId, subjectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
        // Verify the response contains isEnrolled property
        var responseType = okResult.Value!.GetType();
        var isEnrolledProperty = responseType.GetProperty("isEnrolled");
        Assert.NotNull(isEnrolledProperty);
        var isEnrolledValue = isEnrolledProperty.GetValue(okResult.Value);
        Assert.NotNull(isEnrolledValue);
        Assert.True((bool)isEnrolledValue);
        _mockService.Verify(s => s.IsStudentEnrolledInSectionSubjectAsync(studentId, sectionId, subjectId), Times.Once);
    }

    [Fact]
    public async Task CheckEnrollment_StudentNotEnrolled_ReturnsOkWithFalse()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        _mockService.Setup(s => s.IsStudentEnrolledInSectionSubjectAsync(studentId, sectionId, subjectId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CheckEnrollment(studentId, sectionId, subjectId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
        var responseType = okResult.Value!.GetType();
        var isEnrolledProperty = responseType.GetProperty("isEnrolled");
        Assert.NotNull(isEnrolledProperty);
        var isEnrolledValue = isEnrolledProperty.GetValue(okResult.Value);
        Assert.NotNull(isEnrolledValue);
        Assert.False((bool)isEnrolledValue);
    }

    #endregion
}

