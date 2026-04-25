using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Services_Testing;

public class CourseServiceTest
{
    private readonly Mock<ICourseRepository> _mockCourseRepository;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<ILogger<CourseService>> _mockLogger;
    private readonly CourseService _service;
    private readonly ClaimsPrincipal _testUserPrincipal;

    public CourseServiceTest()
    {
        _mockCourseRepository = new Mock<ICourseRepository>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<CourseService>>();
        _service = new CourseService(_mockCourseRepository.Object, _mockUserContextService.Object, _mockLogger.Object);

        _testUserPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Name, "Test User"),
        }, "TestAuth"));
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new CourseService(null!, _mockUserContextService.Object, _mockLogger.Object));

        Assert.Equal("courseRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullUserContextService_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new CourseService(_mockCourseRepository.Object, null!, _mockLogger.Object));

        Assert.Equal("userContextService", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new CourseService(_mockCourseRepository.Object, _mockUserContextService.Object, null!));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public async Task GetAllCoursesAsync_ReturnsMaterializedCourses()
    {
        _mockCourseRepository
            .Setup(repository => repository.GetAllCoursesAsync())
            .ReturnsAsync(new List<Course>
            {
                new() { Id = 1, Name = "Math" },
                new() { Id = 2, Name = "Science" },
            });

        var result = await _service.GetAllCoursesAsync();

        var materialized = Assert.IsType<List<Course>>(result);
        Assert.Collection(materialized,
            course => Assert.Equal("Math", course.Name),
            course => Assert.Equal("Science", course.Name));
    }

    [Fact]
    public async Task GetAllCoursesAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockCourseRepository
            .Setup(repository => repository.GetAllCoursesAsync())
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetAllCoursesAsync());

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("GetAllCourses", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task GetCourseByIdAsync_ReturnsCourse_WhenFound()
    {
        var course = new Course { Id = 4, Name = "Physics" };
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(4))
            .ReturnsAsync(course);

        var result = await _service.GetCourseByIdAsync(4);

        Assert.Same(course, result);
    }

    [Fact]
    public async Task GetCourseByIdAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(4))
            .ReturnsAsync((Course?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.GetCourseByIdAsync(4));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal(4, exception.Key);
    }

    [Fact]
    public async Task GetCourseByIdAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(4))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetCourseByIdAsync(4));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("GetCourseById: 4", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task GetCourseByUuidAsync_ReturnsCourse_WhenFound()
    {
        var courseUuid = Guid.NewGuid();
        var course = new Course { Id = 4, Uuid = courseUuid, Name = "Physics" };
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByUuidAsync(courseUuid))
            .ReturnsAsync(course);

        var result = await _service.GetCourseByUuidAsync(courseUuid);

        Assert.Same(course, result);
    }

    [Fact]
    public async Task GetCourseByUuidAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        var courseUuid = Guid.NewGuid();
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByUuidAsync(courseUuid))
            .ReturnsAsync((Course?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.GetCourseByUuidAsync(courseUuid));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal(courseUuid, exception.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateCourseAsync_BlankName_ThrowsEntityServiceException(string? name)
    {
        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.CreateCourseAsync(new CreateCourse { Name = name! }, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("CreateCourse", exception.Operation);
        Assert.Contains("Course name is required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCourseAsync_MissingUserId_ThrowsEntityServiceException()
    {
        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync((string?)null);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.CreateCourseAsync(new CreateCourse { Name = "Physics" }, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("CreateCourse", exception.Operation);
        Assert.Contains("User ID not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCourseAsync_ValidInput_CreatesCourse()
    {
        Course? capturedCourse = null;
        var createdCourse = new Course { Id = 10, Name = "Physics" };

        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCourseRepository
            .Setup(repository => repository.CreateCourse(It.IsAny<Course>()))
            .Callback<Course>(course => capturedCourse = course)
            .ReturnsAsync(createdCourse);
        _mockCourseRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _service.CreateCourseAsync(new CreateCourse { Name = "Physics" }, _testUserPrincipal);

        Assert.Same(createdCourse, result);
        Assert.NotNull(capturedCourse);
        Assert.Equal("Physics", capturedCourse.Name);
        _mockCourseRepository.Verify(repository => repository.CreateCourse(It.IsAny<Course>()), Times.Once);
        _mockCourseRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateCourseAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Create failed");
        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCourseRepository
            .Setup(repository => repository.CreateCourse(It.IsAny<Course>()))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.CreateCourseAsync(new CreateCourse { Name = "Physics" }, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("CreateCourse", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task UpdateCourseAsync_NullUpdateDto_ThrowsEntityServiceException()
    {
        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.UpdateCourseAsync(4, null!, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("UpdateCourse: 4", exception.Operation);
        Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateCourseAsync_MissingUserId_ThrowsEntityServiceException()
    {
        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync(string.Empty);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.UpdateCourseAsync(4, new UpdateCourse { Name = "New" }, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("UpdateCourse: 4", exception.Operation);
        Assert.Contains("User ID not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateCourseAsync_NotFound_ThrowsEntityNotFoundException()
    {
        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(4))
            .ReturnsAsync((Course?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(
            () => _service.UpdateCourseAsync(4, new UpdateCourse { Name = "New" }, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal(4, exception.Key);
    }

    [Fact]
    public async Task UpdateCourseAsync_ValidInput_UpdatesCourse()
    {
        var existingCourse = new Course { Id = 4, Name = "Old" };

        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(4))
            .ReturnsAsync(existingCourse);
        _mockCourseRepository
            .Setup(repository => repository.UpdateCourseAsync(It.IsAny<Course>()))
            .ReturnsAsync((Course course) => course);
        _mockCourseRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _service.UpdateCourseAsync(4, new UpdateCourse { Name = "New" }, _testUserPrincipal);

        Assert.Equal("New", result.Name);
        _mockCourseRepository.Verify(repository => repository.UpdateCourseAsync(It.IsAny<Course>()), Times.Once);
        _mockCourseRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseAsync_EmptyName_LeavesCourseNameUnchanged()
    {
        var existingCourse = new Course { Id = 4, Name = "Old" };

        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(4))
            .ReturnsAsync(existingCourse);
        _mockCourseRepository
            .Setup(repository => repository.UpdateCourseAsync(It.IsAny<Course>()))
            .ReturnsAsync((Course course) => course);
        _mockCourseRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _service.UpdateCourseAsync(4, new UpdateCourse { Name = string.Empty }, _testUserPrincipal);

        Assert.Equal("Old", result.Name);
    }

    [Fact]
    public async Task UpdateCourseAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Update failed");
        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(4))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.UpdateCourseAsync(4, new UpdateCourse { Name = "New" }, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("UpdateCourse: 4", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task DeleteCourseAsync_MissingUserId_ThrowsEntityServiceException()
    {
        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync((string?)null);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.DeleteCourseAsync(9, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("DeleteCourse: 9", exception.Operation);
        Assert.Contains("User ID not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteCourseAsync_NotFound_ThrowsEntityNotFoundException()
    {
        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(9))
            .ReturnsAsync((Course?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.DeleteCourseAsync(9, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal(9, exception.Key);
    }

    [Fact]
    public async Task DeleteCourseAsync_Success_DeletesCourse()
    {
        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(9))
            .ReturnsAsync(new Course { Id = 9, Name = "Biology" });
        _mockCourseRepository
            .Setup(repository => repository.DeleteCourseAsync(9))
            .ReturnsAsync(true);
        _mockCourseRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        await _service.DeleteCourseAsync(9, _testUserPrincipal);

        _mockCourseRepository.Verify(repository => repository.DeleteCourseAsync(9), Times.Once);
        _mockCourseRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteCourseAsync_DeleteReturnsFalse_ThrowsEntityServiceException()
    {
        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(9))
            .ReturnsAsync(new Course { Id = 9, Name = "Biology" });
        _mockCourseRepository
            .Setup(repository => repository.DeleteCourseAsync(9))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.DeleteCourseAsync(9, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("DeleteCourse: 9", exception.Operation);
        Assert.Contains("Failed to delete course", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("The DELETE statement conflicted with the REFERENCE constraint \"FK_Sections_Courses\"", "sections assigned")]
    [InlineData("23503: update or delete on table \"Courses\" violates foreign key constraint \"FK_Unknown\"", "dependencies that prevent deletion")]
    public async Task DeleteCourseAsync_ForeignKeyViolation_ThrowsEntityConflictException(string innerMessage, string expectedMessagePart)
    {
        var dbUpdateException = new DbUpdateException("Delete failed", new Exception(innerMessage));

        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(9))
            .ReturnsAsync(new Course { Id = 9, Name = "Biology" });
        _mockCourseRepository
            .Setup(repository => repository.DeleteCourseAsync(9))
            .ReturnsAsync(true);
        _mockCourseRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.DeleteCourseAsync(9, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("sections", exception.ConflictType);
        Assert.Contains(expectedMessagePart, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteCourseAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Delete failed");
        _mockUserContextService
            .Setup(service => service.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCourseRepository
            .Setup(repository => repository.GetCourseByIdAsync(9))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.DeleteCourseAsync(9, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("DeleteCourse: 9", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task HasSectionsInCourseAsync_ReturnsRepositoryResult()
    {
        _mockCourseRepository
            .Setup(repository => repository.HasSectionsInCourseAsync(2))
            .ReturnsAsync(true);

        var result = await _service.HasSectionsInCourseAsync(2);

        Assert.True(result);
    }

    [Fact]
    public async Task HasSectionsInCourseAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockCourseRepository
            .Setup(repository => repository.HasSectionsInCourseAsync(2))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.HasSectionsInCourseAsync(2));

        Assert.Equal("Course", exception.EntityName);
        Assert.Equal("HasSectionsInCourse: 2", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }
}
