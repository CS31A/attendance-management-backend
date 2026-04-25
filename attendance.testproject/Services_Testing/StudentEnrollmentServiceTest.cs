using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Unit tests for StudentEnrollmentService
/// Tests all public methods with happy paths, edge cases, and error scenarios
/// </summary>
public class StudentEnrollmentServiceTest
{
    private readonly Mock<IStudentEnrollmentRepository> _mockEnrollmentRepo;
    private readonly Mock<IStudentRepository> _mockStudentRepo;
    private readonly Mock<ISectionRepository> _mockSectionRepo;
    private readonly Mock<ISubjectRepository> _mockSubjectRepo;
    private readonly Mock<ILogger<StudentEnrollmentService>> _mockLogger;
    private readonly StudentEnrollmentService _service;

    public StudentEnrollmentServiceTest()
    {
        _mockEnrollmentRepo = new Mock<IStudentEnrollmentRepository>();
        _mockStudentRepo = new Mock<IStudentRepository>();
        _mockSectionRepo = new Mock<ISectionRepository>();
        _mockSubjectRepo = new Mock<ISubjectRepository>();
        _mockLogger = new Mock<ILogger<StudentEnrollmentService>>();

        _service = new StudentEnrollmentService(
            _mockEnrollmentRepo.Object,
            _mockStudentRepo.Object,
            _mockSectionRepo.Object,
            _mockSubjectRepo.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public void Constructor_NullDependency_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StudentEnrollmentService(null!, _mockStudentRepo.Object, _mockSectionRepo.Object, _mockSubjectRepo.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new StudentEnrollmentService(_mockEnrollmentRepo.Object, null!, _mockSectionRepo.Object, _mockSubjectRepo.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new StudentEnrollmentService(_mockEnrollmentRepo.Object, _mockStudentRepo.Object, null!, _mockSubjectRepo.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new StudentEnrollmentService(_mockEnrollmentRepo.Object, _mockStudentRepo.Object, _mockSectionRepo.Object, null!, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new StudentEnrollmentService(_mockEnrollmentRepo.Object, _mockStudentRepo.Object, _mockSectionRepo.Object, _mockSubjectRepo.Object, null!));
    }

    #region EnrollStudentAsync Tests

    [Fact]
    public async Task EnrollStudentAsync_ValidData_CreatesNewEnrollment()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        var student = new Student { Id = studentId, SectionId = 5, IsDeleted = false };
        var section = new Section { Id = sectionId };
        var subject = new Subject { Id = subjectId };
        var expectedEnrollment = new StudentEnrollment
        {
            Id = 1,
            StudentId = studentId,
            SectionId = sectionId,
            SubjectId = subjectId,
            IsActive = true,
            EnrollmentType = "Irregular"
        };

        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(sectionId)).ReturnsAsync(section);
        _mockSubjectRepo.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(subject);
        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId))
            .ReturnsAsync((StudentEnrollment?)null);
        _mockEnrollmentRepo.Setup(r => r.CreateAsync(It.IsAny<StudentEnrollment>()))
            .ReturnsAsync(expectedEnrollment);

        // Act
        var result = await _service.EnrollStudentAsync(studentId, sectionId, subjectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(studentId, result.StudentId);
        Assert.Equal(sectionId, result.SectionId);
        Assert.Equal(subjectId, result.SubjectId);
        Assert.True(result.IsActive);
        _mockEnrollmentRepo.Verify(r => r.CreateAsync(It.IsAny<StudentEnrollment>()), Times.Once);
    }

    [Fact]
    public async Task EnrollStudentAsync_RequestWithCanonicalGuidIds_NormalizesToIntIdentifiers()
    {
        var studentUuid = Guid.NewGuid();
        var sectionUuid = Guid.NewGuid();
        var subjectUuid = Guid.NewGuid();
        var student = new Student { Id = 1, Uuid = studentUuid, SectionId = 5, IsDeleted = false };
        var section = new Section { Id = 2, Uuid = sectionUuid };
        var subject = new Subject { Id = 3, Uuid = subjectUuid };

        _mockStudentRepo.Setup(r => r.GetStudentByUuidAsync(studentUuid)).ReturnsAsync(student);
        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(student.Id)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByUuidAsync(sectionUuid)).ReturnsAsync(section);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(section.Id)).ReturnsAsync(section);
        _mockSubjectRepo.Setup(r => r.GetSubjectByUuidAsync(subjectUuid)).ReturnsAsync(subject);
        _mockSubjectRepo.Setup(r => r.GetSubjectByIdAsync(subject.Id)).ReturnsAsync(subject);
        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(student.Id, section.Id, subject.Id))
            .ReturnsAsync((StudentEnrollment?)null);
        _mockEnrollmentRepo.Setup(r => r.CreateAsync(It.IsAny<StudentEnrollment>()))
            .ReturnsAsync((StudentEnrollment e) => e);

        var result = await _service.EnrollStudentAsync(new CreateStudentEnrollment
        {
            StudentId = studentUuid,
            SectionId = sectionUuid,
            SubjectId = subjectUuid,
            EnrollmentType = "Irregular",
        });

        Assert.Equal(student.Id, result.StudentId);
        Assert.Equal(section.Id, result.SectionId);
        Assert.Equal(subject.Id, result.SubjectId);
    }

    [Fact]
    public async Task GetEnrollmentByUuidAsync_ReturnsEnrollment_WhenFound()
    {
        var enrollmentUuid = Guid.NewGuid();
        var enrollment = new StudentEnrollment { Id = 10, Uuid = enrollmentUuid, StudentId = 1, SectionId = 2, SubjectId = 3 };
        _mockEnrollmentRepo.Setup(r => r.GetByUuidAsync(enrollmentUuid)).ReturnsAsync(enrollment);

        var result = await _service.GetEnrollmentByUuidAsync(enrollmentUuid);

        Assert.Same(enrollment, result);
    }

    [Fact]
    public async Task EnrollStudentAsync_WithOptionalParameters_CreatesEnrollmentWithAllFields()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        var enrollmentType = "Retake";
        var academicYear = "2024-2025";
        var semester = "First";
        var student = new Student { Id = studentId, SectionId = 5, IsDeleted = false };
        var section = new Section { Id = sectionId };
        var subject = new Subject { Id = subjectId };

        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(sectionId)).ReturnsAsync(section);
        _mockSubjectRepo.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(subject);
        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId))
            .ReturnsAsync((StudentEnrollment?)null);
        _mockEnrollmentRepo.Setup(r => r.CreateAsync(It.IsAny<StudentEnrollment>()))
            .ReturnsAsync((StudentEnrollment e) => e);

        // Act
        var result = await _service.EnrollStudentAsync(studentId, sectionId, subjectId,
            enrollmentType, academicYear, semester);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(enrollmentType, result.EnrollmentType);
        Assert.Equal(academicYear, result.AcademicYear);
        Assert.Equal(semester, result.Semester);
    }

    [Theory]
    [InlineData("Regular")]
    [InlineData("Irregular")]
    [InlineData("Retake")]
    [InlineData("regular")]
    public async Task EnrollStudentAsync_KnownEnrollmentTypes_AcceptsAndNormalizes(string enrollmentType)
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        var student = new Student { Id = studentId, SectionId = 5, IsDeleted = false };
        var section = new Section { Id = sectionId };
        var subject = new Subject { Id = subjectId };

        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(sectionId)).ReturnsAsync(section);
        _mockSubjectRepo.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(subject);
        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId))
            .ReturnsAsync((StudentEnrollment?)null);
        _mockEnrollmentRepo.Setup(r => r.CreateAsync(It.IsAny<StudentEnrollment>()))
            .ReturnsAsync((StudentEnrollment e) => e);

        // Act
        var result = await _service.EnrollStudentAsync(studentId, sectionId, subjectId, enrollmentType);

        // Assert
        Assert.Equal(EnrollmentTypeConstants.Normalize(enrollmentType), result.EnrollmentType);
    }

    [Fact]
    public async Task EnrollStudentAsync_UnknownEnrollmentType_ThrowsValidationException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _service.EnrollStudentAsync(1, 2, 3, "Elective"));

        Assert.Equal("Enrollment type must be one of: Regular, Irregular, Retake", exception.Message);
    }

    [Fact]
    public async Task EnrollStudentAsync_InactiveEnrollmentExists_ReactivatesEnrollment()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        var student = new Student { Id = studentId, SectionId = 5, IsDeleted = false };
        var section = new Section { Id = sectionId };
        var subject = new Subject { Id = subjectId };
        var existingEnrollment = new StudentEnrollment
        {
            Id = 10,
            StudentId = studentId,
            SectionId = sectionId,
            SubjectId = subjectId,
            IsActive = false
        };
        var reactivatedEnrollment = new StudentEnrollment
        {
            Id = 10,
            StudentId = studentId,
            SectionId = sectionId,
            SubjectId = subjectId,
            IsActive = true
        };

        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(sectionId)).ReturnsAsync(section);
        _mockSubjectRepo.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(subject);
        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId))
            .ReturnsAsync(existingEnrollment);
        _mockEnrollmentRepo.Setup(r => r.ReactivateEnrollmentAsync(existingEnrollment.Id))
            .ReturnsAsync(true);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(existingEnrollment.Id))
            .ReturnsAsync(reactivatedEnrollment);

        // Act
        var result = await _service.EnrollStudentAsync(studentId, sectionId, subjectId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsActive);
        _mockEnrollmentRepo.Verify(r => r.ReactivateEnrollmentAsync(existingEnrollment.Id), Times.Once);
        _mockEnrollmentRepo.Verify(r => r.CreateAsync(It.IsAny<StudentEnrollment>()), Times.Never);
    }

    [Fact]
    public async Task EnrollStudentAsync_StudentNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var studentId = 999;
        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId))
            .ReturnsAsync((Student?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<int>>(
            () => _service.EnrollStudentAsync(studentId, 1, 1));
    }

    [Fact]
    public async Task EnrollStudentAsync_StudentIsDeleted_ThrowsEntityNotFoundException()
    {
        // Arrange
        var studentId = 1;
        var deletedStudent = new Student { Id = studentId, IsDeleted = true };
        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(deletedStudent);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<int>>(
            () => _service.EnrollStudentAsync(studentId, 1, 1));
    }

    [Fact]
    public async Task EnrollStudentAsync_SectionNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 999;
        var student = new Student { Id = studentId, SectionId = 5, IsDeleted = false };

        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(sectionId))
            .ReturnsAsync((Section?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<int>>(
            () => _service.EnrollStudentAsync(studentId, sectionId, 1));
    }

    [Fact]
    public async Task EnrollStudentAsync_UnexpectedSectionRepositoryFailure_LogsContextAndRethrows()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        var student = new Student { Id = studentId, SectionId = 5, IsDeleted = false };
        var expectedException = new InvalidOperationException("Section lookup failed");

        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(sectionId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.EnrollStudentAsync(studentId, sectionId, subjectId));

        // Assert
        Assert.Same(expectedException, exception);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("student 1")
                                                && v.ToString()!.Contains("section 2")
                                                && v.ToString()!.Contains("subject 3")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task EnrollStudentAsync_SubjectNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 999;
        var student = new Student { Id = studentId, SectionId = 5, IsDeleted = false };
        var section = new Section { Id = sectionId };

        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(sectionId)).ReturnsAsync(section);
        _mockSubjectRepo.Setup(r => r.GetSubjectByIdAsync(subjectId))
            .ReturnsAsync((Subject?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<int>>(
            () => _service.EnrollStudentAsync(studentId, sectionId, subjectId));
    }

    [Fact]
    public async Task EnrollStudentAsync_StudentPrimarySection_ThrowsEntityAlreadyExistsException()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 5; // Same as student's primary section
        var subjectId = 3;
        var student = new Student { Id = studentId, SectionId = sectionId, IsDeleted = false };
        var section = new Section { Id = sectionId };
        var subject = new Subject { Id = subjectId };

        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(sectionId)).ReturnsAsync(section);
        _mockSubjectRepo.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(subject);

        // Act & Assert
        await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(
            () => _service.EnrollStudentAsync(studentId, sectionId, subjectId));
    }

    [Fact]
    public async Task EnrollStudentAsync_ActiveEnrollmentExists_ThrowsEntityAlreadyExistsException()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        var student = new Student { Id = studentId, SectionId = 5, IsDeleted = false };
        var section = new Section { Id = sectionId };
        var subject = new Subject { Id = subjectId };
        var activeEnrollment = new StudentEnrollment
        {
            Id = 10,
            StudentId = studentId,
            SectionId = sectionId,
            SubjectId = subjectId,
            IsActive = true
        };

        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(sectionId)).ReturnsAsync(section);
        _mockSubjectRepo.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(subject);
        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId))
            .ReturnsAsync(activeEnrollment);

        // Act & Assert
        await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(
            () => _service.EnrollStudentAsync(studentId, sectionId, subjectId));
    }

    [Fact]
    public async Task EnrollStudentAsync_ReactivationFails_FallsBackToExistingEnrollment()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        var student = new Student { Id = studentId, SectionId = 5, IsDeleted = false };
        var section = new Section { Id = sectionId };
        var subject = new Subject { Id = subjectId };
        var existingEnrollment = new StudentEnrollment
        {
            Id = 10,
            StudentId = studentId,
            SectionId = sectionId,
            SubjectId = subjectId,
            IsActive = false
        };

        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(sectionId)).ReturnsAsync(section);
        _mockSubjectRepo.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(subject);
        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId))
            .ReturnsAsync(existingEnrollment);
        _mockEnrollmentRepo.Setup(r => r.ReactivateEnrollmentAsync(existingEnrollment.Id))
            .ReturnsAsync(true);
        _mockEnrollmentRepo.Setup(r => r.GetByIdAsync(existingEnrollment.Id))
            .ReturnsAsync((StudentEnrollment?)null); // Reload fails

        // Act
        var result = await _service.EnrollStudentAsync(studentId, sectionId, subjectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingEnrollment.Id, result.Id); // Falls back to existing
    }

    #endregion

    #region UnenrollStudentAsync Tests

    [Fact]
    public async Task UnenrollStudentAsync_EnrollmentExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        var enrollment = new StudentEnrollment { Id = 10, StudentId = studentId };

        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId))
            .ReturnsAsync(enrollment);
        _mockEnrollmentRepo.Setup(r => r.DeleteAsync(enrollment.Id))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UnenrollStudentAsync(studentId, sectionId, subjectId);

        // Assert
        Assert.True(result);
        _mockEnrollmentRepo.Verify(r => r.DeleteAsync(enrollment.Id), Times.Once);
    }

    [Fact]
    public async Task UnenrollStudentAsync_EnrollmentNotFound_ReturnsFalse()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;

        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId))
            .ReturnsAsync((StudentEnrollment?)null);

        // Act
        var result = await _service.UnenrollStudentAsync(studentId, sectionId, subjectId);

        // Assert
        Assert.False(result);
        _mockEnrollmentRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region DropStudentFromSubjectAsync Tests

    [Fact]
    public async Task DropStudentFromSubjectAsync_ValidId_CallsRepositoryDeactivate()
    {
        // Arrange
        var enrollmentId = 10;
        _mockEnrollmentRepo.Setup(r => r.DeactivateEnrollmentAsync(enrollmentId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DropStudentFromSubjectAsync(enrollmentId);

        // Assert
        Assert.True(result);
        _mockEnrollmentRepo.Verify(r => r.DeactivateEnrollmentAsync(enrollmentId), Times.Once);
    }

    #endregion

    #region ReenrollStudentAsync Tests

    [Fact]
    public async Task ReenrollStudentAsync_ValidId_CallsRepositoryReactivate()
    {
        // Arrange
        var enrollmentId = 10;
        _mockEnrollmentRepo.Setup(r => r.ReactivateEnrollmentAsync(enrollmentId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ReenrollStudentAsync(enrollmentId);

        // Assert
        Assert.True(result);
        _mockEnrollmentRepo.Verify(r => r.ReactivateEnrollmentAsync(enrollmentId), Times.Once);
    }

    #endregion

    #region GetStudentEnrollmentsAsync Tests

    [Fact]
    public async Task GetStudentEnrollmentsAsync_ValidStudentId_ReturnsEnrollments()
    {
        // Arrange
        var studentId = 1;
        var enrollments = new List<StudentEnrollment>
        {
            new StudentEnrollment { Id = 1, StudentId = studentId, IsActive = true },
            new StudentEnrollment { Id = 2, StudentId = studentId, IsActive = false }
        };

        _mockEnrollmentRepo.Setup(r => r.GetStudentEnrollmentsAsync(studentId))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _service.GetStudentEnrollmentsAsync(studentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockEnrollmentRepo.Verify(r => r.GetStudentEnrollmentsAsync(studentId), Times.Once);
    }

    #endregion

    #region GetActiveStudentEnrollmentsAsync Tests

    [Fact]
    public async Task GetActiveStudentEnrollmentsAsync_ValidStudentId_ReturnsActiveEnrollments()
    {
        // Arrange
        var studentId = 1;
        var activeEnrollments = new List<StudentEnrollment>
        {
            new StudentEnrollment { Id = 1, StudentId = studentId, IsActive = true },
            new StudentEnrollment { Id = 2, StudentId = studentId, IsActive = true }
        };

        _mockEnrollmentRepo.Setup(r => r.GetActiveEnrollmentsAsync(studentId))
            .ReturnsAsync(activeEnrollments);

        // Act
        var result = await _service.GetActiveStudentEnrollmentsAsync(studentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, e => Assert.True(e.IsActive));
        _mockEnrollmentRepo.Verify(r => r.GetActiveEnrollmentsAsync(studentId), Times.Once);
    }

    #endregion

    #region GetStudentSectionsAsync Tests

    [Fact]
    public async Task GetStudentSectionsAsync_ValidStudentId_ReturnsSections()
    {
        // Arrange
        var studentId = 1;
        var sections = new List<Section>
        {
            new Section { Id = 1, Name = "CS-3A" },
            new Section { Id = 2, Name = "CS-4B" }
        };

        _mockEnrollmentRepo.Setup(r => r.GetStudentSectionsAsync(studentId))
            .ReturnsAsync(sections);

        // Act
        var result = await _service.GetStudentSectionsAsync(studentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockEnrollmentRepo.Verify(r => r.GetStudentSectionsAsync(studentId), Times.Once);
    }

    #endregion

    #region GetStudentSubjectsAsync Tests

    [Fact]
    public async Task GetStudentSubjectsAsync_ValidStudentId_ReturnsSubjects()
    {
        // Arrange
        var studentId = 1;
        var subjects = new List<Subject>
        {
            new Subject { Id = 1, Name = "Mathematics" },
            new Subject { Id = 2, Name = "Physics" }
        };

        _mockEnrollmentRepo.Setup(r => r.GetStudentSubjectsAsync(studentId))
            .ReturnsAsync(subjects);

        // Act
        var result = await _service.GetStudentSubjectsAsync(studentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockEnrollmentRepo.Verify(r => r.GetStudentSubjectsAsync(studentId), Times.Once);
    }

    #endregion

    #region IsStudentEnrolledInSectionSubjectAsync Tests

    [Fact]
    public async Task IsStudentEnrolledInSectionSubjectAsync_Enrolled_ReturnsTrue()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;

        _mockEnrollmentRepo.Setup(r => r.IsStudentEnrolledAsync(studentId, sectionId, subjectId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.IsStudentEnrolledInSectionSubjectAsync(studentId, sectionId, subjectId);

        // Assert
        Assert.True(result);
        _mockEnrollmentRepo.Verify(r => r.IsStudentEnrolledAsync(studentId, sectionId, subjectId), Times.Once);
    }

    [Fact]
    public async Task IsStudentEnrolledInSectionSubjectAsync_NotEnrolled_ReturnsFalse()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;

        _mockEnrollmentRepo.Setup(r => r.IsStudentEnrolledAsync(studentId, sectionId, subjectId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.IsStudentEnrolledInSectionSubjectAsync(studentId, sectionId, subjectId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetStudentsInSectionAsync Tests

    [Fact]
    public async Task GetStudentsInSectionAsync_ValidSectionId_ReturnsDistinctStudents()
    {
        // Arrange
        var sectionId = 1;
        var student1 = new Student { Id = 1, Firstname = "John" };
        var student2 = new Student { Id = 2, Firstname = "Jane" };
        var enrollments = new List<StudentEnrollment>
        {
            new StudentEnrollment { Id = 1, SectionId = sectionId, Student = student1 },
            new StudentEnrollment { Id = 2, SectionId = sectionId, Student = student2 },
            new StudentEnrollment { Id = 3, SectionId = sectionId, Student = student1 } // Duplicate
        };

        _mockEnrollmentRepo.Setup(r => r.GetActiveSectionEnrollmentsAsync(sectionId))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _service.GetStudentsInSectionAsync(sectionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count()); // Should be distinct
        _mockEnrollmentRepo.Verify(r => r.GetActiveSectionEnrollmentsAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task GetStudentsInSectionAsync_NoEnrollments_ReturnsEmptyList()
    {
        // Arrange
        var sectionId = 1;
        var enrollments = new List<StudentEnrollment>();

        _mockEnrollmentRepo.Setup(r => r.GetActiveSectionEnrollmentsAsync(sectionId))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _service.GetStudentsInSectionAsync(sectionId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetStudentsInSubjectAsync Tests

    [Fact]
    public async Task GetStudentsInSubjectAsync_ValidSubjectId_ReturnsDistinctStudents()
    {
        // Arrange
        var subjectId = 1;
        var student1 = new Student { Id = 1, Firstname = "John" };
        var student2 = new Student { Id = 2, Firstname = "Jane" };
        var enrollments = new List<StudentEnrollment>
        {
            new StudentEnrollment { Id = 1, SubjectId = subjectId, Student = student1 },
            new StudentEnrollment { Id = 2, SubjectId = subjectId, Student = student2 }
        };

        _mockEnrollmentRepo.Setup(r => r.GetActiveSubjectEnrollmentsAsync(subjectId))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _service.GetStudentsInSubjectAsync(subjectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockEnrollmentRepo.Verify(r => r.GetActiveSubjectEnrollmentsAsync(subjectId), Times.Once);
    }

    [Fact]
    public async Task GetStudentsInSubjectAsync_NoEnrollments_ReturnsEmptyList()
    {
        // Arrange
        var subjectId = 1;
        var enrollments = new List<StudentEnrollment>();

        _mockEnrollmentRepo.Setup(r => r.GetActiveSubjectEnrollmentsAsync(subjectId))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _service.GetStudentsInSubjectAsync(subjectId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetSectionEnrollmentsAsync Tests

    [Fact]
    public async Task GetSectionEnrollmentsAsync_ValidSectionId_ReturnsEnrollments()
    {
        // Arrange
        var sectionId = 1;
        var enrollments = new List<StudentEnrollment>
        {
            new StudentEnrollment { Id = 1, SectionId = sectionId, IsActive = true },
            new StudentEnrollment { Id = 2, SectionId = sectionId, IsActive = false }
        };

        _mockEnrollmentRepo.Setup(r => r.GetSectionEnrollmentsAsync(sectionId))
            .ReturnsAsync(enrollments);

        // Act
        var result = await _service.GetSectionEnrollmentsAsync(sectionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockEnrollmentRepo.Verify(r => r.GetSectionEnrollmentsAsync(sectionId), Times.Once);
    }

    #endregion

    #region GetActiveSectionEnrollmentsAsync Tests

    [Fact]
    public async Task GetActiveSectionEnrollmentsAsync_ValidSectionId_ReturnsActiveEnrollments()
    {
        // Arrange
        var sectionId = 1;
        var activeEnrollments = new List<StudentEnrollment>
        {
            new StudentEnrollment { Id = 1, SectionId = sectionId, IsActive = true },
            new StudentEnrollment { Id = 2, SectionId = sectionId, IsActive = true }
        };

        _mockEnrollmentRepo.Setup(r => r.GetActiveSectionEnrollmentsAsync(sectionId))
            .ReturnsAsync(activeEnrollments);

        // Act
        var result = await _service.GetActiveSectionEnrollmentsAsync(sectionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, e => Assert.True(e.IsActive));
        _mockEnrollmentRepo.Verify(r => r.GetActiveSectionEnrollmentsAsync(sectionId), Times.Once);
    }

    #endregion

    #region GetSpecificEnrollmentAsync Tests

    [Fact]
    public async Task GetSpecificEnrollmentAsync_EnrollmentExists_ReturnsEnrollment()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        var enrollment = new StudentEnrollment
        {
            Id = 10,
            StudentId = studentId,
            SectionId = sectionId,
            SubjectId = subjectId,
            IsActive = true
        };

        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId))
            .ReturnsAsync(enrollment);

        // Act
        var result = await _service.GetSpecificEnrollmentAsync(studentId, sectionId, subjectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(enrollment.Id, result.Id);
        Assert.Equal(studentId, result.StudentId);
        Assert.Equal(sectionId, result.SectionId);
        Assert.Equal(subjectId, result.SubjectId);
        _mockEnrollmentRepo.Verify(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId), Times.Once);
    }

    [Fact]
    public async Task GetSpecificEnrollmentAsync_EnrollmentNotFound_ReturnsNull()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;

        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId))
            .ReturnsAsync((StudentEnrollment?)null);

        // Act
        var result = await _service.GetSpecificEnrollmentAsync(studentId, sectionId, subjectId);

        // Assert
        Assert.Null(result);
        _mockEnrollmentRepo.Verify(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId), Times.Once);
    }

    #endregion
}
