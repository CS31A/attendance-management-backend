using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using attendance_monitoring.Services.Crud;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Services_Testing;

public class SubjectServiceTest
{
    private readonly Mock<ISubjectRepository> _mockSubjectRepository;
    private readonly Mock<ICrudService<Subject, CreateSubject, UpdateSubject>> _mockCrudService;
    private readonly Mock<ILogger<SubjectService>> _mockLogger;
    private readonly SubjectService _service;

    public SubjectServiceTest()
    {
        _mockSubjectRepository = new Mock<ISubjectRepository>();
        _mockCrudService = new Mock<ICrudService<Subject, CreateSubject, UpdateSubject>>();
        _mockLogger = new Mock<ILogger<SubjectService>>();
        _service = new SubjectService(_mockCrudService.Object, _mockSubjectRepository.Object, _mockLogger.Object);
    }

    #region GetAllSubjectsAsync

    [Fact]
    public async Task GetAllSubjectsAsync_DelegatesToCrudService()
    {
        var subjects = new List<Subject>
        {
            new() { Id = Guid.NewGuid(), Name = "Mathematics", Code = "MATH101" },
            new() { Id = Guid.NewGuid(), Name = "Physics", Code = "PHYS101" }
        };
        _mockCrudService.Setup(s => s.GetAllAsync()).ReturnsAsync(subjects);

        var result = await _service.GetAllSubjectsAsync();

        Assert.Equal(subjects, result);
        _mockCrudService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetSubjectByIdAsync

    [Fact]
    public async Task GetSubjectByIdAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var subject = new Subject { Id = id, Name = "Mathematics", Code = "MATH101" };
        _mockCrudService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(subject);

        var result = await _service.GetSubjectByIdAsync(id);

        Assert.Same(subject, result);
        _mockCrudService.Verify(s => s.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task GetSubjectByIdAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.GetByIdAsync(id))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Subject", id));

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _service.GetSubjectByIdAsync(id));

        Assert.Equal("Subject", exception.EntityName);
    }

    #endregion

    #region GetSubjectByUuidAsync

    [Fact]
    public async Task GetSubjectByUuidAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var subject = new Subject { Id = id, Name = "Mathematics", Code = "MATH101" };
        _mockCrudService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(subject);

        var result = await _service.GetSubjectByUuidAsync(id);

        Assert.Same(subject, result);
        _mockCrudService.Verify(s => s.GetByIdAsync(id), Times.Once);
    }

    #endregion

    #region CreateSubjectAsync

    [Fact]
    public async Task CreateSubjectAsync_DelegatesToCrudService()
    {
        var dto = new CreateSubject { Name = "Mathematics", Code = "MATH101" };
        var created = new Subject { Id = Guid.NewGuid(), Name = "Mathematics", Code = "MATH101" };
        _mockCrudService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await _service.CreateSubjectAsync(dto);

        Assert.Same(created, result);
        _mockCrudService.Verify(s => s.CreateAsync(dto), Times.Once);
    }

    [Fact]
    public async Task CreateSubjectAsync_PropagatesValidationException()
    {
        var dto = new CreateSubject { Name = "", Code = "MATH101" };
        _mockCrudService.Setup(s => s.CreateAsync(dto))
            .ThrowsAsync(new ValidationException("Code is required"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateSubjectAsync(dto));
    }

    [Fact]
    public async Task CreateSubjectAsync_PropagatesEntityAlreadyExistsException()
    {
        var dto = new CreateSubject { Name = "Mathematics", Code = "MATH101" };
        _mockCrudService.Setup(s => s.CreateAsync(dto))
            .ThrowsAsync(new EntityAlreadyExistsException<string>("Subject", "Code", "MATH101"));

        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(
            () => _service.CreateSubjectAsync(dto));

        Assert.Equal("MATH101", exception.EntityIdentifier);
    }

    #endregion

    #region UpdateSubjectAsync

    [Fact]
    public async Task UpdateSubjectAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateSubject { Name = "Updated", Code = "MATH101" };
        var updated = new Subject { Id = id, Name = "Updated", Code = "MATH101" };
        _mockCrudService.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(updated);

        var result = await _service.UpdateSubjectAsync(id, dto);

        Assert.Same(updated, result);
        _mockCrudService.Verify(s => s.UpdateAsync(id, dto), Times.Once);
    }

    [Fact]
    public async Task UpdateSubjectAsync_PropagatesEntityNotFoundException()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateSubject { Name = "Updated", Code = "MATH101" };
        _mockCrudService.Setup(s => s.UpdateAsync(id, dto))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Subject", id));

        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _service.UpdateSubjectAsync(id, dto));
    }

    #endregion

    #region UpdateSubjectByUuidAsync

    [Fact]
    public async Task UpdateSubjectByUuidAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateSubject { Name = "Updated", Code = "MATH101" };
        var updated = new Subject { Id = id, Name = "Updated", Code = "MATH101" };
        _mockCrudService.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(updated);

        var result = await _service.UpdateSubjectByUuidAsync(id, dto);

        Assert.Same(updated, result);
        _mockCrudService.Verify(s => s.UpdateAsync(id, dto), Times.Once);
    }

    #endregion

    #region DeleteSubjectAsync

    [Fact]
    public async Task DeleteSubjectAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        await _service.DeleteSubjectAsync(id);

        _mockCrudService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteSubjectAsync_PropagatesEntityNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.DeleteAsync(id))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Subject", id));

        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _service.DeleteSubjectAsync(id));
    }

    #endregion

    #region DeleteSubjectByUuidAsync

    [Fact]
    public async Task DeleteSubjectByUuidAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        await _service.DeleteSubjectByUuidAsync(id);

        _mockCrudService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    #endregion

    #region Dependency Check Operations

    [Fact]
    public async Task HasSchedulesInSubjectAsync_ReturnsRepositoryResult()
    {
        _mockSubjectRepository
            .Setup(r => r.HasSchedulesInSubjectAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var result = await _service.HasSchedulesInSubjectAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task HasSchedulesInSubjectAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockSubjectRepository
            .Setup(r => r.HasSchedulesInSubjectAsync(It.IsAny<Guid>()))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.HasSchedulesInSubjectAsync(Guid.NewGuid()));

        Assert.Equal("Subject", exception.EntityName);
        Assert.Contains("HasSchedulesInSubject:", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task HasEnrollmentsInSubjectAsync_ReturnsRepositoryResult()
    {
        _mockSubjectRepository
            .Setup(r => r.HasEnrollmentsInSubjectAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var result = await _service.HasEnrollmentsInSubjectAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task HasEnrollmentsInSubjectAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockSubjectRepository
            .Setup(r => r.HasEnrollmentsInSubjectAsync(It.IsAny<Guid>()))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.HasEnrollmentsInSubjectAsync(Guid.NewGuid()));

        Assert.Equal("Subject", exception.EntityName);
        Assert.Contains("HasEnrollmentsInSubject:", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion
}
