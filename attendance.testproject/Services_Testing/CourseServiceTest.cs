using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using attendance_monitoring.Services.Crud;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Services_Testing;

public class CourseServiceTest
{
    private readonly Mock<ICourseRepository> _mockCourseRepository;
    private readonly Mock<ICrudService<Course, CreateCourse, UpdateCourse>> _mockCrudService;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<ILogger<CourseService>> _mockLogger;
    private readonly CourseService _service;
    private readonly ClaimsPrincipal _testUserPrincipal;

    public CourseServiceTest()
    {
        _mockCourseRepository = new Mock<ICourseRepository>();
        _mockCrudService = new Mock<ICrudService<Course, CreateCourse, UpdateCourse>>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<CourseService>>();
        _service = new CourseService(_mockCrudService.Object, _mockCourseRepository.Object,
            _mockUserContextService.Object, _mockLogger.Object);

        _testUserPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Name, "Test User"),
        }, "TestAuth"));
    }

    #region Read Operations

    [Fact]
    public async Task GetAllCoursesAsync_DelegatesToCrudService()
    {
        var courses = new List<Course>
        {
            new() { Id = Guid.NewGuid(), Name = "Math" },
            new() { Id = Guid.NewGuid(), Name = "Science" },
        };
        _mockCrudService.Setup(s => s.GetAllAsync()).ReturnsAsync(courses);

        var result = await _service.GetAllCoursesAsync();

        Assert.Equal(2, result.Count);
        _mockCrudService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCourseByIdAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var course = new Course { Id = id, Name = "Physics" };
        _mockCrudService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(course);

        var result = await _service.GetCourseByIdAsync(id);

        Assert.Same(course, result);
    }

    [Fact]
    public async Task GetCourseByUuidAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var course = new Course { Id = id, Name = "Physics" };
        _mockCrudService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(course);

        var result = await _service.GetCourseByUuidAsync(id);

        Assert.Same(course, result);
    }

    #endregion

    #region Create Operations

    [Fact]
    public async Task CreateCourseAsync_MissingUserId_ThrowsEntityServiceException()
    {
        _mockUserContextService
            .Setup(s => s.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync((string?)null);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.CreateCourseAsync(new CreateCourse { Name = "Physics" }, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Contains("User ID not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCourseAsync_ValidInput_DelegatesToCrudService()
    {
        var dto = new CreateCourse { Name = "Physics" };
        var created = new Course { Id = Guid.NewGuid(), Name = "Physics" };

        _mockUserContextService
            .Setup(s => s.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCrudService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await _service.CreateCourseAsync(dto, _testUserPrincipal);

        Assert.Same(created, result);
        _mockCrudService.Verify(s => s.CreateAsync(dto), Times.Once);
    }

    #endregion

    #region Update Operations

    [Fact]
    public async Task UpdateCourseAsync_MissingUserId_ThrowsEntityServiceException()
    {
        _mockUserContextService
            .Setup(s => s.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync(string.Empty);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.UpdateCourseAsync(Guid.NewGuid(), new UpdateCourse { Name = "New" }, _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Contains("User ID not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateCourseAsync_ValidInput_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateCourse { Name = "New" };
        var updated = new Course { Id = id, Name = "New" };

        _mockUserContextService
            .Setup(s => s.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCrudService.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(updated);

        var result = await _service.UpdateCourseAsync(id, dto, _testUserPrincipal);

        Assert.Same(updated, result);
        _mockCrudService.Verify(s => s.UpdateAsync(id, dto), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseByUuidAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateCourse { Name = "New" };
        var updated = new Course { Id = id, Name = "New" };

        _mockUserContextService
            .Setup(s => s.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCrudService.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(updated);

        var result = await _service.UpdateCourseByUuidAsync(id, dto, _testUserPrincipal);

        Assert.Same(updated, result);
    }

    #endregion

    #region Delete Operations

    [Fact]
    public async Task DeleteCourseAsync_MissingUserId_ThrowsEntityServiceException()
    {
        _mockUserContextService
            .Setup(s => s.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync((string?)null);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.DeleteCourseAsync(Guid.NewGuid(), _testUserPrincipal));

        Assert.Equal("Course", exception.EntityName);
        Assert.Contains("User ID not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteCourseAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();

        _mockUserContextService
            .Setup(s => s.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCrudService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        await _service.DeleteCourseAsync(id, _testUserPrincipal);

        _mockCrudService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteCourseByUuidAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();

        _mockUserContextService
            .Setup(s => s.GetUserIdAsync(_testUserPrincipal))
            .ReturnsAsync("user-1");
        _mockCrudService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        await _service.DeleteCourseByUuidAsync(id, _testUserPrincipal);

        _mockCrudService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    #endregion

    #region Dependency Check Operations

    [Fact]
    public async Task HasSectionsInCourseAsync_ReturnsRepositoryResult()
    {
        _mockCourseRepository
            .Setup(r => r.HasSectionsInCourseAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var result = await _service.HasSectionsInCourseAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task HasSectionsInCourseAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockCourseRepository
            .Setup(r => r.HasSectionsInCourseAsync(It.IsAny<Guid>()))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.HasSectionsInCourseAsync(Guid.NewGuid()));

        Assert.Equal("Course", exception.EntityName);
        Assert.Contains("HasSectionsInCourse:", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion
}
