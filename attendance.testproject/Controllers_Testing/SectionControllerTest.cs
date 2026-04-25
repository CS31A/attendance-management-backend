using attendance_monitoring.Classes;
using attendance_monitoring.Controllers;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class SectionControllerTest
{
    private readonly Mock<ISectionService> _mockSectionService;
    private readonly Mock<ICourseService> _mockCourseService;
    private readonly Mock<ILogger<SectionController>> _mockLogger;
    private readonly SectionController _controller;

    public SectionControllerTest()
    {
        _mockSectionService = new Mock<ISectionService>();
        _mockCourseService = new Mock<ICourseService>();
        _mockLogger = new Mock<ILogger<SectionController>>();
        _controller = new SectionController(_mockSectionService.Object, _mockCourseService.Object, _mockLogger.Object);
    }

    private static Section CreateSectionEntity(
        int id = 1,
        string name = "Section A",
        int courseId = 10,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        return new Section
        {
            Id = id,
            Name = name,
            CourseId = courseId,
            CreatedAt = createdAt ?? new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = updatedAt ?? new DateTime(2024, 1, 2, 8, 0, 0, DateTimeKind.Utc)
        };
    }

    private static SectionResponseDto CreateSectionResponseDto(
        Guid? id = null,
        string name = "Section A",
        Guid? courseId = null,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        return new SectionResponseDto
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            CourseId = courseId ?? Guid.NewGuid(),
            CreatedAt = createdAt ?? new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = updatedAt ?? new DateTime(2024, 1, 2, 8, 0, 0, DateTimeKind.Utc)
        };
    }

    private static CreateSection CreateSectionRequest(string name = "Section A", Guid? courseId = null)
    {
        return new CreateSection
        {
            Name = name,
            CourseId = courseId ?? Guid.NewGuid()
        };
    }

    private static Student CreateStudent(int id, string firstname, string lastname, string userId, int sectionId = 1)
    {
        return new Student
        {
            Id = id,
            Firstname = firstname,
            Lastname = lastname,
            UserId = userId,
            SectionId = sectionId,
            CreatedAt = new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 2, 8, 0, 0, DateTimeKind.Utc)
        };
    }

    [Fact]
    public async Task GetSection_ReturnsOkResult_WithSectionResponseDto()
    {
        // Arrange
        const int sectionId = 3;
        var section = CreateSectionEntity(id: sectionId, name: "Section 3", courseId: 12);
        section.Uuid = Guid.NewGuid();
        section.Course = new Course { Id = section.CourseId, Uuid = Guid.NewGuid(), Name = "BSCS" };
        _mockSectionService
            .Setup(service => service.GetSectionByIdAsync(sectionId))
            .ReturnsAsync(section);

        // Act
        var result = await _controller.GetSection(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SectionResponseDto>(okResult.Value);
        Assert.Equal(section.Uuid, dto.Id);
        Assert.Equal(section.Name, dto.Name);
        Assert.Equal(section.Course!.Uuid, dto.CourseId);
        Assert.Equal(section.CreatedAt, dto.CreatedAt);
        Assert.Equal(section.UpdatedAt, dto.UpdatedAt);
        _mockSectionService.Verify(service => service.GetSectionByIdAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task GetSectionByUuid_ReturnsOkResult_WithSectionResponseDto()
    {
        var sectionUuid = Guid.NewGuid();
        var courseUuid = Guid.NewGuid();
        var section = CreateSectionEntity(id: 3, name: "Section 3", courseId: 12);
        section.Uuid = sectionUuid;
        section.Course = new Course { Id = 12, Uuid = courseUuid, Name = "BSCS" };

        _mockSectionService
            .Setup(service => service.GetSectionByUuidAsync(sectionUuid))
            .ReturnsAsync(section);

        var result = await _controller.GetSectionByUuid(sectionUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SectionResponseDto>(okResult.Value);
        Assert.Equal(sectionUuid, dto.Id);
        Assert.Equal(courseUuid, dto.CourseId);
    }

    [Fact]
    public async Task GetSectionByUuid_ReturnsNotFound_WhenSectionDoesNotExist()
    {
        var sectionUuid = Guid.NewGuid();
        _mockSectionService
            .Setup(service => service.GetSectionByUuidAsync(sectionUuid))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Section", sectionUuid));

        var result = await _controller.GetSectionByUuid(sectionUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Section with UUID {sectionUuid} not found", notFoundResult.Value);
    }

    [Fact]
    public async Task GetSectionByUuid_ReturnsServerError_WhenServiceThrowsException()
    {
        var sectionUuid = Guid.NewGuid();
        _mockSectionService
            .Setup(service => service.GetSectionByUuidAsync(sectionUuid))
            .ThrowsAsync(new EntityServiceException("Section", $"GetSectionByUuid: {sectionUuid}", "Lookup failed"));

        var result = await _controller.GetSectionByUuid(sectionUuid);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("An error occurred while retrieving the section", objectResult.Value);
    }

    [Fact]
    public async Task GetSection_ReturnsNotFound_WhenSectionDoesNotExist()
    {
        // Arrange
        const int sectionId = 77;
        _mockSectionService
            .Setup(service => service.GetSectionByIdAsync(sectionId))
            .ThrowsAsync(new EntityNotFoundException<int>("Section", sectionId));

        // Act
        var result = await _controller.GetSection(sectionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Section with ID {sectionId} not found", notFoundResult.Value);
    }

    [Fact]
    public async Task GetSection_ReturnsServerError_WhenServiceThrowsException()
    {
        // Arrange
        const int sectionId = 91;
        _mockSectionService
            .Setup(service => service.GetSectionByIdAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", $"GetSection: {sectionId}", "Lookup failed"));

        // Act
        var result = await _controller.GetSection(sectionId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("An error occurred while retrieving the section", objectResult.Value);
    }

    [Fact]
    public async Task GetAllSections_ReturnsOkResult_WithSectionsList()
    {
        // Arrange
        var sections = new List<SectionResponseDto>
        {
            CreateSectionResponseDto(name: "Section A"),
            CreateSectionResponseDto(name: "Section B")
        };
        _mockSectionService
            .Setup(service => service.GetAllSectionsAsync())
            .ReturnsAsync(sections);

        // Act
        var result = await _controller.GetAllSections();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSections = Assert.IsAssignableFrom<IEnumerable<SectionResponseDto>>(okResult.Value);
        Assert.Equal(2, returnedSections.Count());
        Assert.Equal(sections[0].Id, returnedSections.First().Id);
    }

    [Fact]
    public async Task GetAllSections_ReturnsServerError_WhenServiceThrowsException()
    {
        // Arrange
        _mockSectionService
            .Setup(service => service.GetAllSectionsAsync())
            .ThrowsAsync(new EntityServiceException("Section", "GetAllSections", "List failed"));

        // Act
        var result = await _controller.GetAllSections();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("An error occurred while retrieving sections", objectResult.Value);
    }

    [Fact]
    public async Task CreateSection_ReturnsCreatedResult_WhenValidInput()
    {
        // Arrange
        var request = CreateSectionRequest(name: "New Section");
        var createdSection = CreateSectionResponseDto(name: request.Name, courseId: request.CourseId!.Value);

        _mockCourseService
            .Setup(service => service.GetCourseByUuidAsync(request.CourseId!.Value))
            .ReturnsAsync(new Course { Id = 8, Uuid = request.CourseId.Value, Name = "BSCS" });

        _mockSectionService
            .Setup(service => service.CreateSectionAsync(It.Is<Section>(section =>
                section.Name == request.Name &&
                section.CourseId == 8)))
            .ReturnsAsync(createdSection);

        // Act
        var result = await _controller.CreateSection(request);

        // Assert
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(SectionController.GetSectionByUuid), createdAtResult.ActionName);
        Assert.Equal(createdSection.Id, createdAtResult.RouteValues!["id"]);
        var dto = Assert.IsType<SectionResponseDto>(createdAtResult.Value);
        Assert.Equal(createdSection.Id, dto.Id);
        Assert.Equal(request.Name, dto.Name);
    }

    [Fact]
    public async Task CreateSection_ReturnsBadRequest_WhenInvalidModelState()
    {
        // Arrange
        var request = CreateSectionRequest();
        _controller.ModelState.AddModelError("Name", "Section name is required");

        // Act
        var result = await _controller.CreateSection(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
        _mockSectionService.Verify(service => service.CreateSectionAsync(It.IsAny<Section>()), Times.Never);
    }

    [Fact]
    public async Task CreateSection_ReturnsBadRequest_WhenServiceException()
    {
        // Arrange
        var request = CreateSectionRequest(name: "Duplicate Section");
        const string errorMessage = "Section already exists";
        _mockCourseService
            .Setup(service => service.GetCourseByUuidAsync(request.CourseId!.Value))
            .ReturnsAsync(new Course { Id = 1, Uuid = request.CourseId.Value, Name = "BSCS" });
        _mockSectionService
            .Setup(service => service.CreateSectionAsync(It.IsAny<Section>()))
            .ThrowsAsync(new EntityServiceException("Section", "CreateSection", errorMessage));

        // Act
        var result = await _controller.CreateSection(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(errorMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateSection_ReturnsOkResult_WhenUpdateSucceeds()
    {
        // Arrange
        const int sectionId = 15;
        var request = CreateSectionRequest(name: "Updated Section");
        var updatedSection = CreateSectionResponseDto(name: request.Name, courseId: request.CourseId!.Value);

        _mockCourseService
            .Setup(service => service.GetCourseByUuidAsync(request.CourseId!.Value))
            .ReturnsAsync(new Course { Id = 99, Uuid = request.CourseId.Value, Name = "BSIT" });

        _mockSectionService
            .Setup(service => service.UpdateSectionAsync(sectionId, It.Is<Section>(section =>
                section.Name == request.Name &&
                section.CourseId == 99)))
            .ReturnsAsync(updatedSection);

        // Act
        var result = await _controller.UpdateSection(sectionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SectionResponseDto>(okResult.Value);
        Assert.Equal(updatedSection.Id, dto.Id);
        Assert.Equal(request.Name, dto.Name);
        Assert.Equal(request.CourseId, dto.CourseId);
    }

    [Fact]
    public async Task UpdateSectionByUuid_ReturnsOkResult_WhenUuidCourseReferenceSucceeds()
    {
        var sectionUuid = Guid.NewGuid();
        var courseUuid = Guid.NewGuid();
        var request = new CreateSection
        {
            Name = "Updated Section",
            CourseId = courseUuid
        };

        _mockCourseService
            .Setup(service => service.GetCourseByUuidAsync(courseUuid))
            .ReturnsAsync(new Course { Id = 99, Uuid = courseUuid, Name = "BSIT" });

        _mockSectionService
            .Setup(service => service.UpdateSectionByUuidAsync(sectionUuid, It.Is<Section>(section =>
                section.Name == request.Name &&
                section.CourseId == 99)))
            .ReturnsAsync(new SectionResponseDto
            {
                Id = sectionUuid,
                Name = request.Name,
                CourseId = courseUuid
            });

        var result = await _controller.UpdateSectionByUuid(sectionUuid, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SectionResponseDto>(okResult.Value);
        Assert.Equal(sectionUuid, dto.Id);
        Assert.Equal(courseUuid, dto.CourseId);
    }

    [Fact]
    public async Task UpdateSectionByUuid_ReturnsNotFound_WhenSectionDoesNotExist()
    {
        var sectionUuid = Guid.NewGuid();
        var courseUuid = Guid.NewGuid();
        var request = new CreateSection
        {
            Name = "Updated Section",
            CourseId = courseUuid
        };

        _mockCourseService
            .Setup(service => service.GetCourseByUuidAsync(courseUuid))
            .ReturnsAsync(new Course { Id = 99, Uuid = courseUuid, Name = "BSIT" });

        _mockSectionService
            .Setup(service => service.UpdateSectionByUuidAsync(sectionUuid, It.IsAny<Section>()))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Section", sectionUuid));

        var result = await _controller.UpdateSectionByUuid(sectionUuid, request);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateSectionByUuid_ReturnsBadRequest_WhenServiceException()
    {
        var sectionUuid = Guid.NewGuid();
        var courseUuid = Guid.NewGuid();
        var request = new CreateSection
        {
            Name = "Updated Section",
            CourseId = courseUuid
        };
        const string errorMessage = "Unable to update section";

        _mockCourseService
            .Setup(service => service.GetCourseByUuidAsync(courseUuid))
            .ReturnsAsync(new Course { Id = 99, Uuid = courseUuid, Name = "BSIT" });

        _mockSectionService
            .Setup(service => service.UpdateSectionByUuidAsync(sectionUuid, It.IsAny<Section>()))
            .ThrowsAsync(new EntityServiceException("Section", $"UpdateSectionByUuid: {sectionUuid}", errorMessage));

        var result = await _controller.UpdateSectionByUuid(sectionUuid, request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(errorMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateSectionByUuid_ReturnsBadRequest_WhenValidationFails()
    {
        var sectionUuid = Guid.NewGuid();
        var request = new CreateSection
        {
            Name = "Updated Section"
        };

        var result = await _controller.UpdateSectionByUuid(sectionUuid, request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateSection_ReturnsNotFound_WhenSectionDoesNotExist()
    {
        // Arrange
        const int sectionId = 101;
        var request = CreateSectionRequest();
        _mockCourseService
            .Setup(service => service.GetCourseByUuidAsync(request.CourseId!.Value))
            .ReturnsAsync(new Course { Id = 1, Uuid = request.CourseId.Value, Name = "BSCS" });
        _mockSectionService
            .Setup(service => service.UpdateSectionAsync(sectionId, It.IsAny<Section>()))
            .ThrowsAsync(new EntityNotFoundException<int>("Section", sectionId));

        // Act
        var result = await _controller.UpdateSection(sectionId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Section with ID {sectionId} not found", notFoundResult.Value);
    }

    [Fact]
    public async Task UpdateSection_ReturnsBadRequest_WhenServiceException()
    {
        // Arrange
        const int sectionId = 16;
        var request = CreateSectionRequest();
        const string errorMessage = "Unable to update section";
        _mockCourseService
            .Setup(service => service.GetCourseByUuidAsync(request.CourseId!.Value))
            .ReturnsAsync(new Course { Id = 1, Uuid = request.CourseId.Value, Name = "BSCS" });
        _mockSectionService
            .Setup(service => service.UpdateSectionAsync(sectionId, It.IsAny<Section>()))
            .ThrowsAsync(new EntityServiceException("Section", $"UpdateSection: {sectionId}", errorMessage));

        // Act
        var result = await _controller.UpdateSection(sectionId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(errorMessage, badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateSection_ReturnsBadRequest_WhenInvalidModelState()
    {
        // Arrange
        const int sectionId = 5;
        var request = CreateSectionRequest();
        _controller.ModelState.AddModelError("CourseId", "Course ID is required");

        // Act
        var result = await _controller.UpdateSection(sectionId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
        _mockSectionService.Verify(service => service.UpdateSectionAsync(It.IsAny<int>(), It.IsAny<Section>()), Times.Never);
    }

    [Fact]
    public async Task GetActiveStudentsBySectionId_ReturnsOkResult_WithStudentsList()
    {
        // Arrange
        const int sectionId = 4;
        var students = new List<Student>
        {
            CreateStudent(1, "Alice", "Anderson", "user-1", sectionId),
            CreateStudent(2, "Bob", "Brown", "user-2", sectionId)
        };
        _mockSectionService
            .Setup(service => service.GetActiveStudentsBySectionIdAsync(sectionId))
            .ReturnsAsync(students);

        // Act
        var result = await _controller.GetActiveStudentsBySectionId(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudents = Assert.IsAssignableFrom<IEnumerable<Student>>(okResult.Value);
        Assert.Equal(2, returnedStudents.Count());
        Assert.Equal("Alice", returnedStudents.First().Firstname);
    }

    [Fact]
    public async Task GetActiveStudentsBySectionId_ReturnsBadRequest_ForInvalidId()
    {
        // Act
        var result = await _controller.GetActiveStudentsBySectionId(0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Section ID must be greater than 0.", badRequestResult.Value);
        _mockSectionService.Verify(service => service.GetActiveStudentsBySectionIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetActiveStudentsBySectionId_ReturnsServerError_WhenServiceThrowsException()
    {
        // Arrange
        const int sectionId = 6;
        _mockSectionService
            .Setup(service => service.GetActiveStudentsBySectionIdAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", $"GetActiveStudentsBySectionId: {sectionId}", "Lookup failed"));

        // Act
        var result = await _controller.GetActiveStudentsBySectionId(sectionId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("An error occurred while retrieving active students", objectResult.Value);
    }

    [Fact]
    public async Task GetAllStudentsBySectionId_ReturnsOkResult_WithStudentsList()
    {
        // Arrange
        const int sectionId = 5;
        var students = new List<Student>
        {
            CreateStudent(10, "Carla", "Cruz", "user-10", sectionId),
            CreateStudent(11, "Diego", "Diaz", "user-11", sectionId)
        };
        _mockSectionService
            .Setup(service => service.GetAllStudentsBySectionIdAsync(sectionId))
            .ReturnsAsync(students);

        // Act
        var result = await _controller.GetAllStudentsBySectionId(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudents = Assert.IsAssignableFrom<IEnumerable<Student>>(okResult.Value);
        Assert.Equal(2, returnedStudents.Count());
        Assert.Contains(returnedStudents, student => student.Lastname == "Diaz");
    }

    [Fact]
    public async Task GetAllStudentsBySectionId_ReturnsBadRequest_ForInvalidId()
    {
        // Act
        var result = await _controller.GetAllStudentsBySectionId(-1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Section ID must be greater than 0.", badRequestResult.Value);
        _mockSectionService.Verify(service => service.GetAllStudentsBySectionIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAllStudentsBySectionId_ReturnsServerError_WhenServiceThrowsException()
    {
        // Arrange
        const int sectionId = 7;
        _mockSectionService
            .Setup(service => service.GetAllStudentsBySectionIdAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", $"GetAllStudentsBySectionId: {sectionId}", "Lookup failed"));

        // Act
        var result = await _controller.GetAllStudentsBySectionId(sectionId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("An error occurred while retrieving students", objectResult.Value);
    }

    [Fact]
    public async Task HasStudentsInSection_ReturnsOkResult_WithBoolean()
    {
        // Arrange
        const int sectionId = 8;
        _mockSectionService
            .Setup(service => service.HasStudentsInSectionAsync(sectionId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.HasStudentsInSection(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
        _mockSectionService.Verify(service => service.HasStudentsInSectionAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task HasStudentsInSection_ReturnsBadRequest_ForInvalidId()
    {
        // Act
        var result = await _controller.HasStudentsInSection(0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Section ID must be greater than 0.", badRequestResult.Value);
        _mockSectionService.Verify(service => service.HasStudentsInSectionAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task HasStudentsInSection_ReturnsServerError_WhenServiceThrowsException()
    {
        // Arrange
        const int sectionId = 14;
        _mockSectionService
            .Setup(service => service.HasStudentsInSectionAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", $"HasStudentsInSection: {sectionId}", "Dependency check failed"));

        // Act
        var result = await _controller.HasStudentsInSection(sectionId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("An error occurred while checking section dependencies", objectResult.Value);
    }

    [Fact]
    public async Task HasStudentEnrollmentsInSection_ReturnsOkResult_WithBoolean()
    {
        // Arrange
        const int sectionId = 9;
        _mockSectionService
            .Setup(service => service.HasStudentEnrollmentsInSectionAsync(sectionId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.HasStudentEnrollmentsInSection(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.False(Assert.IsType<bool>(okResult.Value));
        _mockSectionService.Verify(service => service.HasStudentEnrollmentsInSectionAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task HasStudentEnrollmentsInSection_ReturnsBadRequest_ForInvalidId()
    {
        // Act
        var result = await _controller.HasStudentEnrollmentsInSection(0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Section ID must be greater than 0.", badRequestResult.Value);
        _mockSectionService.Verify(service => service.HasStudentEnrollmentsInSectionAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task HasStudentEnrollmentsInSection_ReturnsServerError_WhenServiceThrowsException()
    {
        // Arrange
        const int sectionId = 13;
        _mockSectionService
            .Setup(service => service.HasStudentEnrollmentsInSectionAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", $"HasStudentEnrollmentsInSection: {sectionId}", "Dependency check failed"));

        // Act
        var result = await _controller.HasStudentEnrollmentsInSection(sectionId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("An error occurred while checking section dependencies", objectResult.Value);
    }

    [Fact]
    public async Task HasSchedulesInSection_ReturnsOk_WithBooleanResult()
    {
        // Arrange
        const int sectionId = 7;
        _mockSectionService
            .Setup(service => service.HasSchedulesInSectionAsync(sectionId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.HasSchedulesInSection(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
        _mockSectionService.Verify(service => service.HasSchedulesInSectionAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task HasSchedulesInSection_ReturnsBadRequest_ForInvalidId()
    {
        // Act
        var result = await _controller.HasSchedulesInSection(0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Section ID must be greater than 0.", badRequestResult.Value);
        _mockSectionService.Verify(service => service.HasSchedulesInSectionAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task HasSchedulesInSection_ReturnsServerError_WhenServiceThrowsEntityServiceException()
    {
        // Arrange
        const int sectionId = 12;
        _mockSectionService
            .Setup(service => service.HasSchedulesInSectionAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", $"HasSchedulesInSection: {sectionId}", "Error checking section dependencies"));

        // Act
        var result = await _controller.HasSchedulesInSection(sectionId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while checking section dependencies", objectResult.Value);
    }

    [Fact]
    public async Task DeleteSection_ReturnsNoContent_WhenDeletionSucceeds()
    {
        // Arrange
        const int sectionId = 1;
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockSectionService.Verify(service => service.DeleteSectionAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task DeleteSectionByUuid_ReturnsNoContent_WhenDeletionSucceeds()
    {
        var sectionUuid = Guid.NewGuid();
        _mockSectionService
            .Setup(service => service.DeleteSectionByUuidAsync(sectionUuid))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteSectionByUuid(sectionUuid);

        Assert.IsType<NoContentResult>(result);
        _mockSectionService.Verify(service => service.DeleteSectionByUuidAsync(sectionUuid), Times.Once);
    }

    [Fact]
    public async Task DeleteSectionByUuid_ReturnsNotFound_WhenSectionDoesNotExist()
    {
        var sectionUuid = Guid.NewGuid();
        _mockSectionService
            .Setup(service => service.DeleteSectionByUuidAsync(sectionUuid))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Section", sectionUuid));

        var result = await _controller.DeleteSectionByUuid(sectionUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteSectionByUuid_ReturnsConflict_WhenBlockedByDependencies()
    {
        var sectionUuid = Guid.NewGuid();
        const string conflictMessage = "Cannot delete: Section has schedules assigned. Remove schedules first.";
        _mockSectionService
            .Setup(service => service.DeleteSectionByUuidAsync(sectionUuid))
            .ThrowsAsync(new EntityConflictException("Section", "schedules", conflictMessage));

        var result = await _controller.DeleteSectionByUuid(sectionUuid);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(conflictResult.Value);
        Assert.Equal(conflictMessage, errorResponse.Message);
    }

    [Fact]
    public async Task DeleteSectionByUuid_ReturnsServerError_ForUnexpectedServiceException()
    {
        var sectionUuid = Guid.NewGuid();
        _mockSectionService
            .Setup(service => service.DeleteSectionByUuidAsync(sectionUuid))
            .ThrowsAsync(new EntityServiceException("Section", $"DeleteSectionByUuid: {sectionUuid}", "Database connection failed"));

        var result = await _controller.DeleteSectionByUuid(sectionUuid);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("An error occurred while deleting the section", objectResult.Value);
    }

    [Fact]
    public async Task DeleteSection_ReturnsNotFound_WhenSectionDoesNotExist()
    {
        // Arrange
        const int sectionId = 1;
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityNotFoundException<int>("Section", sectionId));

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Section with ID {sectionId} not found", notFoundResult.Value);
    }

    [Fact]
    public async Task DeleteSection_ReturnsConflict_WithErrorResponseDto_WhenBlockedByDependencies()
    {
        // Arrange
        const int sectionId = 1;
        const string conflictMessage = "Cannot delete: Section has schedules assigned. Remove schedules first.";
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityConflictException("Section", "schedules", conflictMessage));

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(conflictResult.Value);
        Assert.False(errorResponse.Success);
        Assert.Equal(conflictMessage, errorResponse.Message);
        Assert.Equal(StatusCodes.Status409Conflict, errorResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteSection_ReturnsConflict_WithEnrollmentsMessage_WhenBlockedByEnrollments()
    {
        // Arrange
        const int sectionId = 1;
        const string conflictMessage = "Cannot delete: Section has student enrollments. Remove enrollments first.";
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityConflictException("Section", "enrollments", conflictMessage));

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(conflictResult.Value);
        Assert.Equal(conflictMessage, errorResponse.Message);
    }

    [Fact]
    public async Task DeleteSection_ReturnsConflict_WithStudentsMessage_WhenBlockedByStudents()
    {
        // Arrange
        const int sectionId = 1;
        const string conflictMessage = "Cannot delete: Section has assigned students. Reassign students first.";
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityConflictException("Section", "students", conflictMessage));

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var errorResponse = Assert.IsType<ErrorResponseDto>(conflictResult.Value);
        Assert.Equal(conflictMessage, errorResponse.Message);
    }

    [Fact]
    public async Task DeleteSection_ReturnsServerError_ForUnexpectedServiceException()
    {
        // Arrange
        const int sectionId = 1;
        _mockSectionService
            .Setup(service => service.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", $"DeleteSection: {sectionId}", "Database connection failed"));

        // Act
        var result = await _controller.DeleteSection(sectionId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while deleting the section", objectResult.Value);
    }
}
