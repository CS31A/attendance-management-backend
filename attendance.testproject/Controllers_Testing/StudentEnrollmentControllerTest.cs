using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Exceptions;
using AppValidationException = attendance_monitoring.Exceptions.ValidationException;

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
            StudentId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            EnrollmentType = "Retake",
            AcademicYear = "2024-2025",
            Semester = "First"
        };

        var enrollment = new StudentEnrollment
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            IsActive = true,
            EnrollmentType = "Retake",
            AcademicYear = "2024-2025",
            Semester = "First",
            EnrolledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Id = Guid.NewGuid(),
            Student = new Student { Id = Guid.NewGuid(), Id = request.StudentId, Firstname = "John", Lastname = "Doe" },
            Section = new Section { Id = Guid.NewGuid(), Id = request.SectionId, Name = "CS-3B" },
            Subject = new Subject { Id = Guid.NewGuid(), Id = request.SubjectId, Name = "Database Systems", Code = "CS301" }
        };

        _mockService.Setup(s => s.EnrollStudentAsync(request))
            .ReturnsAsync(enrollment);

        // Act
        var result = await _controller.EnrollStudent(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudentEnrollmentResponseDto>(okResult.Value);
        Assert.Equal(enrollment.Id, response.Id);
        Assert.Equal(request.StudentId, response.StudentId);
        Assert.Equal("John", response.StudentFirstname);
        Assert.Equal("CS-3B", response.SectionName);
        Assert.Equal("Database Systems", response.SubjectName);
        Assert.Equal("Retake", response.EnrollmentType);
        Assert.True(response.IsActive);
        _mockService.Verify(s => s.EnrollStudentAsync(request), Times.Once);
    }

    [Fact]
    public async Task EnrollStudent_MapsCanonicalGuidIdsInResponse()
    {
        // Arrange
        var request = new CreateStudentEnrollment
        {
            StudentId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            EnrollmentType = "Irregular",
            AcademicYear = "2025-2026",
            Semester = "2nd"
        };

        var enrollment = new StudentEnrollment
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            IsActive = true,
            EnrollmentType = request.EnrollmentType,
            AcademicYear = request.AcademicYear,
            Semester = request.Semester,
            EnrolledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Student = new Student { Id = Guid.NewGuid(), Id = request.StudentId, Firstname = "Slice", Lastname = "A" },
            Section = new Section { Id = Guid.NewGuid(), Id = request.SectionId, Name = "BSCS 3A" },
            Subject = new Subject { Id = Guid.NewGuid(), Id = request.SubjectId, Name = "Algorithms", Code = "CS303" }
        };

        _mockService.Setup(s => s.EnrollStudentAsync(request))
            .ReturnsAsync(enrollment);

        // Act
        var result = await _controller.EnrollStudent(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudentEnrollmentResponseDto>(okResult.Value);
        Assert.NotEqual(Guid.Empty, enrollment.Id);
        Assert.NotEqual(Guid.Empty, enrollment.Student.Id);
        Assert.NotEqual(Guid.Empty, enrollment.Section.Id);
        Assert.NotEqual(Guid.Empty, enrollment.Subject.Id);
        Assert.Equal(enrollment.Id, response.Id);
        Assert.Equal(enrollment.Student!.Id, response.StudentId);
        Assert.Equal(enrollment.Section!.Id, response.SectionId);
        Assert.Equal(enrollment.Subject!.Id, response.SubjectId);
        _mockService.Verify(s => s.EnrollStudentAsync(request), Times.Once);
    }

    [Fact]
    public async Task EnrollStudent_StudentNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateStudentEnrollment
        {
            StudentId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid()
        };

        _mockService.Setup(s => s.EnrollStudentAsync(request))
            .ThrowsAsync(new EntityNotFoundException<int>("Student", 999));

        // Act
        var result = await _controller.EnrollStudent(request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task EnrollStudent_StudentGuidNotFound_ReturnsNotFound()
    {
        var request = new CreateStudentEnrollment
        {
            StudentId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid()
        };

        _mockService.Setup(s => s.EnrollStudentAsync(request))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Student", request.StudentId));

        var result = await _controller.EnrollStudent(request);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task EnrollStudent_DuplicateEnrollment_ReturnsConflict()
    {
        // Arrange
        var request = new CreateStudentEnrollment
        {
            StudentId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid()
        };

        _mockService.Setup(s => s.EnrollStudentAsync(request))
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
            StudentId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid()
        };

        _mockService.Setup(s => s.EnrollStudentAsync(request))
            .ThrowsAsync(new EntityAlreadyExistsException<int>("Enrollment", "Id", 1));

        // Act
        var result = await _controller.EnrollStudent(request);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task EnrollStudent_InvalidEnrollmentTypeFromService_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateStudentEnrollment
        {
            StudentId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            EnrollmentType = "Elective"
        };

        _mockService.Setup(s => s.EnrollStudentAsync(request))
            .ThrowsAsync(new AppValidationException("Enrollment type must be one of: Regular, Irregular, Retake"));

        // Act
        var result = await _controller.EnrollStudent(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public void CreateStudentEnrollment_InvalidEnrollmentType_FailsValidation()
    {
        // Arrange
        var request = new CreateStudentEnrollment
        {
            StudentId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            EnrollmentType = "Elective"
        };
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.ErrorMessage == "Enrollment type must be one of: Regular, Irregular, Retake");
    }

    #endregion

    #region GetStudentEnrollments Tests

    [Fact]
    public async Task GetStudentEnrollments_ValidStudentId_ReturnsOkWithEnrollments()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new Student { Id = studentId, Firstname = "Alice", Lastname = "Smith", Id = Guid.NewGuid() };
        var enrollments = new List<StudentEnrollment>
        {
            new StudentEnrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Student = student,
                SectionId = Guid.NewGuid(),
                SubjectId = Guid.NewGuid(),
                IsActive = true,
                EnrollmentType = "Retake",
                EnrolledAt = DateTime.UtcNow,
                Section = new Section { Id = Guid.NewGuid(), Name = "CS-3B" },
                Subject = new Subject { Id = Guid.NewGuid(), Name = "Database Systems", Code = "CS301" }
            },
            new StudentEnrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                Student = student,
                SectionId = Guid.NewGuid(),
                SubjectId = Guid.NewGuid(),
                IsActive = false,
                EnrollmentType = "Irregular",
                EnrolledAt = DateTime.UtcNow,
                Section = new Section { Id = Guid.NewGuid(), Name = "CS-4A" },
                Subject = new Subject { Id = Guid.NewGuid(), Name = "Machine Learning", Code = "CS402" }
            }
        };

        _mockService.Setup(s => s.GetStudentEnrollmentsAsync(studentId))
            .ReturnsAsync(enrollments);
        _mockService.Setup(s => s.GetStudentByIdAsync(studentId))
            .ReturnsAsync(student);

        // Act
        var result = await _controller.GetStudentEnrollments(studentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudentSectionsResponseDto>(okResult.Value);
        Assert.Equal(enrollments[0].Student!.Id, response.StudentId);
        Assert.Equal(2, response.Enrollments.Count);
        Assert.Equal("CS-3B", response.Enrollments[0].SectionName);
        Assert.Equal("Database Systems", response.Enrollments[0].SubjectName);
        Assert.True(response.Enrollments[0].IsActive);
        Assert.False(response.Enrollments[1].IsActive);
        _mockService.Verify(s => s.GetStudentEnrollmentsAsync(studentId), Times.Once);
    }

    [Fact]
    public async Task GetStudentEnrollmentsByUuid_ValidStudentUuid_ReturnsOkWithEnrollments()
    {
        var studentUuid = Guid.NewGuid();
        var sectionUuid = Guid.NewGuid();
        var subjectUuid = Guid.NewGuid();
        var enrollmentUuid = Guid.NewGuid();
        var enrollments = new List<StudentEnrollment>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Id = enrollmentUuid,
                StudentId = Guid.NewGuid(),
                Student = new Student { Id = Guid.NewGuid(), Id = studentUuid, Firstname = "John", Lastname = "Doe" },
                SectionId = Guid.NewGuid(),
                Section = new Section { Id = Guid.NewGuid(), Id = sectionUuid, Name = "CS-3B" },
                SubjectId = Guid.NewGuid(),
                Subject = new Subject { Id = Guid.NewGuid(), Id = subjectUuid, Name = "Database Systems", Code = "CS301" },
                IsActive = true,
                EnrollmentType = "Retake",
                EnrolledAt = DateTime.UtcNow
            }
        };

        _mockService.Setup(s => s.GetStudentEnrollmentsByStudentUuidAsync(studentUuid))
            .ReturnsAsync(enrollments);

        var result = await _controller.GetStudentEnrollmentsByUuid(studentUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudentSectionsResponseDto>(okResult.Value);
        Assert.Equal(studentUuid, response.StudentId);
        Assert.Equal(enrollmentUuid, response.Enrollments[0].EnrollmentId);
        Assert.Equal(sectionUuid, response.Enrollments[0].SectionId);
        Assert.Equal(subjectUuid, response.Enrollments[0].SubjectId);
    }

    [Fact]
    public async Task GetStudentEnrollmentsByUuid_NoEnrollments_ReturnsOkWithEmptyList()
    {
        var studentUuid = Guid.NewGuid();

        _mockService.Setup(s => s.GetStudentEnrollmentsByStudentUuidAsync(studentUuid))
            .ReturnsAsync(new List<StudentEnrollment>());

        var result = await _controller.GetStudentEnrollmentsByUuid(studentUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudentSectionsResponseDto>(okResult.Value);
        Assert.Equal(studentUuid, response.StudentId);
        Assert.Empty(response.Enrollments);
    }

    [Fact]
    public async Task GetStudentEnrollments_NoEnrollments_ReturnsOkWithEmptyList()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var studentUuid = Guid.NewGuid();
        var enrollments = new List<StudentEnrollment>();

        _mockService.Setup(s => s.GetStudentByIdAsync(studentId))
            .ReturnsAsync(new Student { Id = studentId, Id = studentUuid });
        _mockService.Setup(s => s.GetStudentEnrollmentsAsync(studentId))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _controller.GetStudentEnrollments(studentId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<StudentSectionsResponseDto>(okResult.Value);
        Assert.Equal(studentUuid, response.StudentId);
        Assert.Empty(response.Enrollments);
    }

    [Fact]
    public async Task GetStudentEnrollments_MissingStudent_ReturnsNotFound()
    {
        var studentId = Guid.NewGuid();

        _mockService.Setup(s => s.GetStudentByIdAsync(studentId))
            .ThrowsAsync(new EntityNotFoundException<int>("Student", studentId));

        var result = await _controller.GetStudentEnrollments(studentId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
        _mockService.Verify(s => s.GetStudentEnrollmentsAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region GetSectionStudents Tests

    [Fact]
    public async Task GetSectionStudents_ValidSectionId_ReturnsOkWithStudents()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        var enrollments = new List<StudentEnrollment>
        {
            new StudentEnrollment
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                SectionId = sectionId,
                SubjectId = Guid.NewGuid(),
                IsActive = true,
                EnrollmentType = "Retake",
                EnrolledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Student = new Student { Id = Guid.NewGuid(), Firstname = "John", Lastname = "Doe" },
                Section = new Section { Id = sectionId, Name = "CS-3B" },
                Subject = new Subject { Id = Guid.NewGuid(), Name = "Database Systems", Code = "CS301" }
            },
            new StudentEnrollment
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                SectionId = sectionId,
                SubjectId = Guid.NewGuid(),
                IsActive = true,
                EnrollmentType = "Irregular",
                EnrolledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Student = new Student { Id = Guid.NewGuid(), Firstname = "Jane", Lastname = "Smith" },
                Section = new Section { Id = sectionId, Name = "CS-3B" },
                Subject = new Subject { Id = Guid.NewGuid(), Name = "Web Development", Code = "CS302" }
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
    public async Task GetSectionStudentsByUuid_ValidSectionUuid_ReturnsOkWithStudents()
    {
        var sectionUuid = Guid.NewGuid();
        var studentUuid = Guid.NewGuid();
        var subjectUuid = Guid.NewGuid();
        var enrollments = new List<StudentEnrollment>
        {
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Student = new Student { Id = Guid.NewGuid(), Id = studentUuid, Firstname = "John", Lastname = "Doe" },
                SectionId = Guid.NewGuid(),
                Section = new Section { Id = Guid.NewGuid(), Id = sectionUuid, Name = "CS-3B" },
                SubjectId = Guid.NewGuid(),
                Subject = new Subject { Id = Guid.NewGuid(), Id = subjectUuid, Name = "Database Systems", Code = "CS301" },
                IsActive = true,
                EnrollmentType = "Retake",
                EnrolledAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        };

        _mockService.Setup(s => s.GetActiveSectionEnrollmentsBySectionUuidAsync(sectionUuid))
            .ReturnsAsync(enrollments);

        var result = await _controller.GetSectionStudentsByUuid(sectionUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IEnumerable<StudentEnrollmentResponseDto>>(okResult.Value).ToList();
        Assert.Equal(sectionUuid, response[0].SectionId);
        Assert.Equal(studentUuid, response[0].StudentId);
        Assert.Equal(subjectUuid, response[0].SubjectId);
    }

    [Fact]
    public async Task GetSectionStudentsByUuid_NoEnrollments_ReturnsOkWithEmptyList()
    {
        var sectionUuid = Guid.NewGuid();

        _mockService.Setup(s => s.GetActiveSectionEnrollmentsBySectionUuidAsync(sectionUuid))
            .ReturnsAsync(new List<StudentEnrollment>());

        var result = await _controller.GetSectionStudentsByUuid(sectionUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IEnumerable<StudentEnrollmentResponseDto>>(okResult.Value);
        Assert.Empty(response);
    }

    [Fact]
    public async Task GetSectionStudents_NoEnrollments_ReturnsOkWithEmptyList()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
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
        var enrollmentId = Guid.NewGuid();
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
    public async Task DropStudentByUuid_ValidEnrollmentUuid_ReturnsOkWithSuccessMessage()
    {
        var enrollmentUuid = Guid.NewGuid();
        _mockService.Setup(s => s.DropStudentFromSubjectAsync(enrollmentUuid))
            .ReturnsAsync(true);

        var result = await _controller.DropStudentByUuid(enrollmentUuid);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockService.Verify(s => s.DropStudentFromSubjectAsync(enrollmentUuid), Times.Once);
    }

    [Fact]
    public async Task DropStudentByUuid_ServiceThrowsEntityNotFoundException_ReturnsNotFound()
    {
        var enrollmentUuid = Guid.NewGuid();
        _mockService.Setup(s => s.DropStudentFromSubjectAsync(enrollmentUuid))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Enrollment", enrollmentUuid));

        var result = await _controller.DropStudentByUuid(enrollmentUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task DropStudentByUuid_EnrollmentNotFound_ReturnsNotFound()
    {
        var enrollmentUuid = Guid.NewGuid();
        _mockService.Setup(s => s.DropStudentFromSubjectAsync(enrollmentUuid))
            .ReturnsAsync(false);

        var result = await _controller.DropStudentByUuid(enrollmentUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task DropStudent_EnrollmentNotFound_ReturnsNotFound()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
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
        var enrollmentId = Guid.NewGuid();
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
        var enrollmentId = Guid.NewGuid();
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
    public async Task ReenrollStudentByUuid_ValidEnrollmentUuid_ReturnsOkWithSuccessMessage()
    {
        var enrollmentUuid = Guid.NewGuid();
        _mockService.Setup(s => s.ReenrollStudentAsync(enrollmentUuid))
            .ReturnsAsync(true);

        var result = await _controller.ReenrollStudentByUuid(enrollmentUuid);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockService.Verify(s => s.ReenrollStudentAsync(enrollmentUuid), Times.Once);
    }

    [Fact]
    public async Task ReenrollStudentByUuid_ServiceThrowsEntityNotFoundException_ReturnsNotFound()
    {
        var enrollmentUuid = Guid.NewGuid();
        _mockService.Setup(s => s.ReenrollStudentAsync(enrollmentUuid))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Enrollment", enrollmentUuid));

        var result = await _controller.ReenrollStudentByUuid(enrollmentUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task ReenrollStudentByUuid_EnrollmentNotFound_ReturnsNotFound()
    {
        var enrollmentUuid = Guid.NewGuid();
        _mockService.Setup(s => s.ReenrollStudentAsync(enrollmentUuid))
            .ReturnsAsync(false);

        var result = await _controller.ReenrollStudentByUuid(enrollmentUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task ReenrollStudent_EnrollmentNotFound_ReturnsNotFound()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
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
        var enrollmentId = Guid.NewGuid();
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
        var studentId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
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
        var studentId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
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

    [Fact]
    public async Task CheckEnrollmentByUuid_StudentIsEnrolled_ReturnsOkWithTrue()
    {
        var studentUuid = Guid.NewGuid();
        var sectionUuid = Guid.NewGuid();
        var subjectUuid = Guid.NewGuid();

        _mockService.Setup(s => s.IsStudentEnrolledInSectionSubjectByUuidAsync(studentUuid, sectionUuid, subjectUuid))
            .ReturnsAsync(true);

        var result = await _controller.CheckEnrollmentByUuid(studentUuid, sectionUuid, subjectUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseType = okResult.Value!.GetType();
        var isEnrolledProperty = responseType.GetProperty("isEnrolled");
        Assert.NotNull(isEnrolledProperty);
        Assert.True((bool)isEnrolledProperty.GetValue(okResult.Value)!);
    }

    [Fact]
    public async Task CheckEnrollmentByUuid_StudentNotEnrolled_ReturnsOkWithFalse()
    {
        var studentUuid = Guid.NewGuid();
        var sectionUuid = Guid.NewGuid();
        var subjectUuid = Guid.NewGuid();

        _mockService.Setup(s => s.IsStudentEnrolledInSectionSubjectByUuidAsync(studentUuid, sectionUuid, subjectUuid))
            .ReturnsAsync(false);

        var result = await _controller.CheckEnrollmentByUuid(studentUuid, sectionUuid, subjectUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var responseType = okResult.Value!.GetType();
        var isEnrolledProperty = responseType.GetProperty("isEnrolled");
        Assert.NotNull(isEnrolledProperty);
        Assert.False((bool)isEnrolledProperty.GetValue(okResult.Value)!);
    }

    #endregion
}
