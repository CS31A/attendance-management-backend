using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace attendance.testproject.Controllers_Testing;

/// <summary>
/// Test suite for SectionController endpoints
/// </summary>
public class SectionControllerTest
{
    private readonly Mock<ISectionService> _mockSectionService;
    private readonly Mock<ILogger<SectionController>> _mockLogger;
    private readonly SectionController _sectionController;

    public SectionControllerTest()
    {
        _mockSectionService = new Mock<ISectionService>();
        _mockLogger = new Mock<ILogger<SectionController>>();
        _sectionController = new SectionController(_mockSectionService.Object, _mockLogger.Object);
    }

    #region GetSection Tests

    [Fact]
    public async Task GetSection_ReturnsOkResult_WithSectionData()
    {
        // Arrange
        var sectionId = 1;
        var expectedSection = new Section
        {
            Id = sectionId,
            Name = "BSIT 3-1",
            CourseId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockSectionService
            .Setup(s => s.GetSectionByIdAsync(sectionId))
            .ReturnsAsync(expectedSection);

        // Act
        var result = await _sectionController.GetSection(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sectionResponse = Assert.IsType<SectionResponseDto>(okResult.Value);
        Assert.Equal(sectionId, sectionResponse.Id);
        Assert.Equal("BSIT 3-1", sectionResponse.Name);
        Assert.Equal(1, sectionResponse.CourseId);

        _mockSectionService.Verify(s => s.GetSectionByIdAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task GetSection_ReturnsNotFound_WhenSectionDoesNotExist()
    {
        // Arrange
        var sectionId = 999;
        _mockSectionService
            .Setup(s => s.GetSectionByIdAsync(sectionId))
            .ThrowsAsync(new EntityNotFoundException<int>("Section", sectionId));

        // Act
        var result = await _sectionController.GetSection(sectionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Section with ID {sectionId} not found", notFoundResult.Value);

        _mockSectionService.Verify(s => s.GetSectionByIdAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task GetSection_ReturnsInternalServerError_WhenServiceExceptionOccurs()
    {
        // Arrange
        var sectionId = 1;
        _mockSectionService
            .Setup(s => s.GetSectionByIdAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", "GetSectionById", "Database connection error"));

        // Act
        var result = await _sectionController.GetSection(sectionId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while retrieving the section", statusCodeResult.Value);

        _mockSectionService.Verify(s => s.GetSectionByIdAsync(sectionId), Times.Once);
    }

    #endregion

    #region GetAllSections Tests

    [Fact]
    public async Task GetAllSections_ReturnsOkResult_WithSectionsList()
    {
        // Arrange
        var expectedSections = new List<SectionResponseDto>
        {
            new SectionResponseDto { Id = 1, Name = "BSIT 3-1", CourseId = 1 },
            new SectionResponseDto { Id = 2, Name = "BSCS 2-A", CourseId = 2 }
        };

        _mockSectionService
            .Setup(s => s.GetAllSectionsAsync())
            .ReturnsAsync(expectedSections);

        // Act
        var result = await _sectionController.GetAllSections();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sections = Assert.IsAssignableFrom<IEnumerable<SectionResponseDto>>(okResult.Value);
        Assert.Equal(2, sections.Count());

        _mockSectionService.Verify(s => s.GetAllSectionsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllSections_ReturnsInternalServerError_WhenServiceExceptionOccurs()
    {
        // Arrange
        _mockSectionService
            .Setup(s => s.GetAllSectionsAsync())
            .ThrowsAsync(new EntityServiceException("Section", "GetAllSections", "Database error"));

        // Act
        var result = await _sectionController.GetAllSections();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);

        _mockSectionService.Verify(s => s.GetAllSectionsAsync(), Times.Once);
    }

    #endregion

    #region CreateSection Tests

    [Fact]
    public async Task CreateSection_ReturnsCreatedResult_WithNewSection()
    {
        // Arrange
        var createSectionDto = new CreateSection
        {
            Name = "BSIT 4-1",
            CourseId = 1
        };

        var createdSection = new SectionResponseDto
        {
            Id = 1,
            Name = createSectionDto.Name,
            CourseId = createSectionDto.CourseId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockSectionService
            .Setup(s => s.CreateSectionAsync(It.IsAny<Section>()))
            .ReturnsAsync(createdSection);

        // Act
        var result = await _sectionController.CreateSection(createSectionDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var sectionResponse = Assert.IsType<SectionResponseDto>(createdResult.Value);
        Assert.Equal("BSIT 4-1", sectionResponse.Name);
        Assert.Equal(1, sectionResponse.CourseId);

        _mockSectionService.Verify(s => s.CreateSectionAsync(It.Is<Section>(
            sec => sec.Name == createSectionDto.Name && sec.CourseId == createSectionDto.CourseId
        )), Times.Once);
    }

    [Fact]
    public async Task CreateSection_ReturnsBadRequest_WhenServiceExceptionOccurs()
    {
        // Arrange
        var createSectionDto = new CreateSection
        {
            Name = "BSIT 4-1",
            CourseId = 999
        };

        _mockSectionService
            .Setup(s => s.CreateSectionAsync(It.IsAny<Section>()))
            .ThrowsAsync(new EntityServiceException("Section", "CreateSection", "Course does not exist"));

        // Act
        var result = await _sectionController.CreateSection(createSectionDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Course does not exist", badRequestResult.Value);

        _mockSectionService.Verify(s => s.CreateSectionAsync(It.IsAny<Section>()), Times.Once);
    }

    #endregion

    #region UpdateSection Tests

    [Fact]
    public async Task UpdateSection_ReturnsOkResult_WithUpdatedSection()
    {
        // Arrange
        var sectionId = 1;
        var updateSectionDto = new CreateSection
        {
            Name = "BSIT 3-1 Updated",
            CourseId = 1
        };

        var updatedSection = new SectionResponseDto
        {
            Id = sectionId,
            Name = updateSectionDto.Name,
            CourseId = updateSectionDto.CourseId,
            UpdatedAt = DateTime.UtcNow
        };

        _mockSectionService
            .Setup(s => s.UpdateSectionAsync(sectionId, It.IsAny<Section>()))
            .ReturnsAsync(updatedSection);

        // Act
        var result = await _sectionController.UpdateSection(sectionId, updateSectionDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var sectionResponse = Assert.IsType<SectionResponseDto>(okResult.Value);
        Assert.Equal("BSIT 3-1 Updated", sectionResponse.Name);

        _mockSectionService.Verify(s => s.UpdateSectionAsync(sectionId, It.IsAny<Section>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSection_ReturnsNotFound_WhenSectionDoesNotExist()
    {
        // Arrange
        var sectionId = 999;
        var updateSectionDto = new CreateSection
        {
            Name = "BSIT 3-1",
            CourseId = 1
        };

        _mockSectionService
            .Setup(s => s.UpdateSectionAsync(sectionId, It.IsAny<Section>()))
            .ThrowsAsync(new EntityNotFoundException<int>("Section", sectionId));

        // Act
        var result = await _sectionController.UpdateSection(sectionId, updateSectionDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Section with ID {sectionId} not found", notFoundResult.Value);

        _mockSectionService.Verify(s => s.UpdateSectionAsync(sectionId, It.IsAny<Section>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSection_ReturnsBadRequest_WhenServiceExceptionOccurs()
    {
        // Arrange
        var sectionId = 1;
        var updateSectionDto = new CreateSection
        {
            Name = "BSIT 3-1",
            CourseId = 999
        };

        _mockSectionService
            .Setup(s => s.UpdateSectionAsync(sectionId, It.IsAny<Section>()))
            .ThrowsAsync(new EntityServiceException("Section", "UpdateSection", "Course does not exist"));

        // Act
        var result = await _sectionController.UpdateSection(sectionId, updateSectionDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        _mockSectionService.Verify(s => s.UpdateSectionAsync(sectionId, It.IsAny<Section>()), Times.Once);
    }

    #endregion

    #region DeleteSection Tests

    [Fact]
    public async Task DeleteSection_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var sectionId = 1;
        _mockSectionService
            .Setup(s => s.DeleteSectionAsync(sectionId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sectionController.DeleteSection(sectionId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        _mockSectionService.Verify(s => s.DeleteSectionAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task DeleteSection_ReturnsNotFound_WhenSectionDoesNotExist()
    {
        // Arrange
        var sectionId = 999;
        _mockSectionService
            .Setup(s => s.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityNotFoundException<int>("Section", sectionId));

        // Act
        var result = await _sectionController.DeleteSection(sectionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Section with ID {sectionId} not found", notFoundResult.Value);

        _mockSectionService.Verify(s => s.DeleteSectionAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task DeleteSection_ReturnsInternalServerError_WhenServiceExceptionOccurs()
    {
        // Arrange
        var sectionId = 1;
        _mockSectionService
            .Setup(s => s.DeleteSectionAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", "DeleteSection", "Database error"));

        // Act
        var result = await _sectionController.DeleteSection(sectionId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);

        _mockSectionService.Verify(s => s.DeleteSectionAsync(sectionId), Times.Once);
    }

    #endregion

    #region GetActiveStudentsBySectionId Tests

    [Fact]
    public async Task GetActiveStudentsBySectionId_ReturnsOkResult_WithStudentsList()
    {
        // Arrange
        var sectionId = 1;
        var expectedStudents = new List<Student>
        {
            new Student
            {
                Id = 1,
                Firstname = "John",
                Lastname = "Doe",
                Email = "john@example.com",
                UserId = "user1",
                SectionId = sectionId,
                IsRegular = true,
                IsDeleted = false
            },
            new Student
            {
                Id = 2,
                Firstname = "Jane",
                Lastname = "Smith",
                Email = "jane@example.com",
                UserId = "user2",
                SectionId = sectionId,
                IsRegular = true,
                IsDeleted = false
            }
        };

        _mockSectionService
            .Setup(s => s.GetActiveStudentsBySectionIdAsync(sectionId))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _sectionController.GetActiveStudentsBySectionId(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IEnumerable<Student>>(okResult.Value);
        Assert.Equal(2, students.Count());

        _mockSectionService.Verify(s => s.GetActiveStudentsBySectionIdAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task GetActiveStudentsBySectionId_ReturnsBadRequest_WhenSectionIdIsInvalid()
    {
        // Arrange
        var invalidSectionId = 0;

        // Act
        var result = await _sectionController.GetActiveStudentsBySectionId(invalidSectionId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Section ID must be greater than 0.", badRequestResult.Value);

        _mockSectionService.Verify(s => s.GetActiveStudentsBySectionIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetActiveStudentsBySectionId_ReturnsInternalServerError_WhenServiceExceptionOccurs()
    {
        // Arrange
        var sectionId = 1;
        _mockSectionService
            .Setup(s => s.GetActiveStudentsBySectionIdAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", "GetActiveStudents", "Database error"));

        // Act
        var result = await _sectionController.GetActiveStudentsBySectionId(sectionId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);

        _mockSectionService.Verify(s => s.GetActiveStudentsBySectionIdAsync(sectionId), Times.Once);
    }

    #endregion

    #region GetAllStudentsBySectionId Tests

    [Fact]
    public async Task GetAllStudentsBySectionId_ReturnsOkResult_WithStudentsList()
    {
        // Arrange
        var sectionId = 1;
        var expectedStudents = new List<Student>
        {
            new Student
            {
                Id = 1,
                Firstname = "John",
                Lastname = "Doe",
                Email = "john@example.com",
                UserId = "user1",
                SectionId = sectionId,
                IsRegular = true,
                IsDeleted = false
            },
            new Student
            {
                Id = 2,
                Firstname = "Jane",
                Lastname = "Smith",
                Email = "jane@example.com",
                UserId = "user2",
                SectionId = sectionId,
                IsRegular = true,
                IsDeleted = false
            },
            new Student
            {
                Id = 3,
                Firstname = "Bob",
                Lastname = "Johnson",
                Email = "bob@example.com",
                UserId = "user3",
                SectionId = sectionId,
                IsRegular = false,
                IsDeleted = true
            }
        };

        _mockSectionService
            .Setup(s => s.GetAllStudentsBySectionIdAsync(sectionId))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _sectionController.GetAllStudentsBySectionId(sectionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IEnumerable<Student>>(okResult.Value);
        Assert.Equal(3, students.Count());

        _mockSectionService.Verify(s => s.GetAllStudentsBySectionIdAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task GetAllStudentsBySectionId_ReturnsBadRequest_WhenSectionIdIsInvalid()
    {
        // Arrange
        var invalidSectionId = -5;

        // Act
        var result = await _sectionController.GetAllStudentsBySectionId(invalidSectionId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Section ID must be greater than 0.", badRequestResult.Value);

        _mockSectionService.Verify(s => s.GetAllStudentsBySectionIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAllStudentsBySectionId_ReturnsInternalServerError_WhenServiceExceptionOccurs()
    {
        // Arrange
        var sectionId = 1;
        _mockSectionService
            .Setup(s => s.GetAllStudentsBySectionIdAsync(sectionId))
            .ThrowsAsync(new EntityServiceException("Section", "GetAllStudents", "Database connection failed"));

        // Act
        var result = await _sectionController.GetAllStudentsBySectionId(sectionId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while retrieving students", statusCodeResult.Value);

        _mockSectionService.Verify(s => s.GetAllStudentsBySectionIdAsync(sectionId), Times.Once);
    }

    #endregion
}