using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services;
using attendance_monitoring.Services.Crud;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Services_Testing;

public class SectionServiceTest
{
    private readonly Mock<ISectionRepository> _mockSectionRepository;
    private readonly Mock<ICrudService<Section, Section, Section>> _mockCrudService;
    private readonly Mock<ILogger<SectionService>> _mockLogger;
    private readonly SectionService _service;

    public SectionServiceTest()
    {
        _mockSectionRepository = new Mock<ISectionRepository>();
        _mockCrudService = new Mock<ICrudService<Section, Section, Section>>();
        _mockLogger = new Mock<ILogger<SectionService>>();
        _service = new SectionService(_mockCrudService.Object, _mockSectionRepository.Object, _mockLogger.Object);
    }

    #region Read Operations

    [Fact]
    public async Task GetSectionByIdAsync_ReturnsSectionFromRepository()
    {
        var id = Guid.NewGuid();
        var section = new Section { Id = id, Name = "CS-3A" };
        _mockSectionRepository.Setup(r => r.GetSectionByIdAsync(id)).ReturnsAsync(section);

        var result = await _service.GetSectionByIdAsync(id);

        Assert.Same(section, result);
    }

    [Fact]
    public async Task GetSectionByIdAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        var id = Guid.NewGuid();
        _mockSectionRepository.Setup(r => r.GetSectionByIdAsync(id)).ReturnsAsync((Section?)null);

        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.GetSectionByIdAsync(id));
    }

    [Fact]
    public async Task GetSectionByUuidAsync_ReturnsSectionFromRepository()
    {
        var id = Guid.NewGuid();
        var section = new Section { Id = id, Name = "CS-3A" };
        _mockSectionRepository.Setup(r => r.GetSectionByUuidAsync(id)).ReturnsAsync(section);

        var result = await _service.GetSectionByUuidAsync(id);

        Assert.Same(section, result);
    }

    [Fact]
    public async Task GetSectionByUuidAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        var id = Guid.NewGuid();
        _mockSectionRepository.Setup(r => r.GetSectionByUuidAsync(id)).ReturnsAsync((Section?)null);

        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.GetSectionByUuidAsync(id));
    }

    [Fact]
    public async Task GetAllSectionsAsync_ReturnsMappedDtos()
    {
        var sections = new List<Section>
        {
            new() { Id = Guid.NewGuid(), Name = "CS-3A", Course = new Course { Id = Guid.NewGuid() } },
            new() { Id = Guid.NewGuid(), Name = "CS-3B", Course = new Course { Id = Guid.NewGuid() } }
        };
        _mockSectionRepository.Setup(r => r.GetAllSectionsAsync()).ReturnsAsync(sections);

        var result = await _service.GetAllSectionsAsync();

        var dtos = result.ToList();
        Assert.Equal(2, dtos.Count);
        Assert.Equal("CS-3A", dtos[0].Name);
        Assert.Equal(sections[0].Course.Id, dtos[0].CourseId);
    }

    #endregion

    #region Create Operations

    [Fact]
    public async Task CreateSectionAsync_DelegatesAndMapsToDto()
    {
        var section = new Section { Name = "CS-3A", CourseId = Guid.NewGuid() };
        var created = new Section { Id = Guid.NewGuid(), Name = "CS-3A" };
        var refreshed = new Section { Id = created.Id, Name = "CS-3A", Course = new Course { Id = section.CourseId } };

        _mockCrudService.Setup(s => s.CreateAsync(section)).ReturnsAsync(created);
        _mockSectionRepository.Setup(r => r.GetSectionByIdAsync(created.Id)).ReturnsAsync(refreshed);

        var result = await _service.CreateSectionAsync(section);

        Assert.IsType<SectionResponseDto>(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(refreshed.Course.Id, result.CourseId);
    }

    #endregion

    #region Update Operations

    [Fact]
    public async Task UpdateSectionAsync_DelegatesAndMapsToDto()
    {
        var id = Guid.NewGuid();
        var section = new Section { Name = "Updated", CourseId = Guid.NewGuid() };
        var updated = new Section { Id = id, Name = "Updated" };
        var refreshed = new Section { Id = id, Name = "Updated", Course = new Course { Id = section.CourseId } };

        _mockCrudService.Setup(s => s.UpdateAsync(id, section)).ReturnsAsync(updated);
        _mockSectionRepository.Setup(r => r.GetSectionByIdAsync(id)).ReturnsAsync(refreshed);

        var result = await _service.UpdateSectionAsync(id, section);

        Assert.IsType<SectionResponseDto>(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task UpdateSectionByUuidAsync_DelegatesAndMapsToDto()
    {
        var id = Guid.NewGuid();
        var section = new Section { Name = "Updated", CourseId = Guid.NewGuid() };
        var updated = new Section { Id = id, Name = "Updated" };
        var refreshed = new Section { Id = id, Name = "Updated", Course = new Course { Id = section.CourseId } };

        _mockCrudService.Setup(s => s.UpdateAsync(id, section)).ReturnsAsync(updated);
        _mockSectionRepository.Setup(r => r.GetSectionByIdAsync(id)).ReturnsAsync(refreshed);

        var result = await _service.UpdateSectionByUuidAsync(id, section);

        Assert.IsType<SectionResponseDto>(result);
    }

    #endregion

    #region Delete Operations

    [Fact]
    public async Task DeleteSectionAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        await _service.DeleteSectionAsync(id);

        _mockCrudService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteSectionByUuidAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        await _service.DeleteSectionByUuidAsync(id);

        _mockCrudService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    #endregion

    #region Student Operations

    [Fact]
    public async Task GetActiveStudentsBySectionIdAsync_ReturnsRepositoryResult()
    {
        var sectionId = Guid.NewGuid();
        var students = new List<Student> { new() { Id = Guid.NewGuid() } };
        _mockSectionRepository.Setup(r => r.GetActiveStudentsBySectionIdAsync(sectionId)).ReturnsAsync(students);

        var result = await _service.GetActiveStudentsBySectionIdAsync(sectionId);

        Assert.Equal(students, result);
    }

    [Fact]
    public async Task GetAllStudentsBySectionIdAsync_ReturnsRepositoryResult()
    {
        var sectionId = Guid.NewGuid();
        var students = new List<Student> { new() { Id = Guid.NewGuid() } };
        _mockSectionRepository.Setup(r => r.GetAllStudentsBySectionIdAsync(sectionId)).ReturnsAsync(students);

        var result = await _service.GetAllStudentsBySectionIdAsync(sectionId);

        Assert.Equal(students, result);
    }

    #endregion

    #region Dependency Check Operations

    [Fact]
    public async Task HasStudentsInSectionAsync_ReturnsRepositoryResult()
    {
        _mockSectionRepository.Setup(r => r.HasStudentsInSectionAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var result = await _service.HasStudentsInSectionAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task HasStudentsInSectionAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockSectionRepository.Setup(r => r.HasStudentsInSectionAsync(It.IsAny<Guid>())).ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.HasStudentsInSectionAsync(Guid.NewGuid()));

        Assert.Equal("Section", exception.EntityName);
        Assert.Contains("HasStudentsInSection:", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task HasStudentEnrollmentsInSectionAsync_ReturnsRepositoryResult()
    {
        _mockSectionRepository.Setup(r => r.HasStudentEnrollmentsInSectionAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var result = await _service.HasStudentEnrollmentsInSectionAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task HasStudentEnrollmentsInSectionAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockSectionRepository.Setup(r => r.HasStudentEnrollmentsInSectionAsync(It.IsAny<Guid>())).ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.HasStudentEnrollmentsInSectionAsync(Guid.NewGuid()));

        Assert.Equal("Section", exception.EntityName);
        Assert.Contains("HasStudentEnrollmentsInSection:", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task HasSchedulesInSectionAsync_ReturnsRepositoryResult()
    {
        _mockSectionRepository.Setup(r => r.HasSchedulesInSectionAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var result = await _service.HasSchedulesInSectionAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task HasSchedulesInSectionAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockSectionRepository.Setup(r => r.HasSchedulesInSectionAsync(It.IsAny<Guid>())).ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.HasSchedulesInSectionAsync(Guid.NewGuid()));

        Assert.Equal("Section", exception.EntityName);
        Assert.Contains("HasSchedulesInSection:", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion
}
