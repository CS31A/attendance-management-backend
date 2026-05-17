using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services;
using Moq;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Unit tests for InstructorService facade.
/// Verifies delegation to sub-services (IInstructorCrudService, IInstructorQueryService, IInstructorDetailService).
/// </summary>
public class InstructorServiceTest
{
    private readonly Mock<IInstructorCrudService> _mockCrudService;
    private readonly Mock<IInstructorQueryService> _mockQueryService;
    private readonly Mock<IInstructorDetailService> _mockDetailService;
    private readonly InstructorService _service;
    private readonly ClaimsPrincipal _testUserPrincipal;

    public InstructorServiceTest()
    {
        _mockCrudService = new Mock<IInstructorCrudService>();
        _mockQueryService = new Mock<IInstructorQueryService>();
        _mockDetailService = new Mock<IInstructorDetailService>();

        _service = new InstructorService(
            _mockCrudService.Object,
            _mockQueryService.Object,
            _mockDetailService.Object
        );

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        _testUserPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullCrudService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new InstructorService(null!, _mockQueryService.Object, _mockDetailService.Object));
    }

    [Fact]
    public void Constructor_NullQueryService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new InstructorService(_mockCrudService.Object, null!, _mockDetailService.Object));
    }

    [Fact]
    public void Constructor_NullDetailService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new InstructorService(_mockCrudService.Object, _mockQueryService.Object, null!));
    }

    #endregion

    #region CRUD Delegation Tests

    [Fact]
    public async Task GetAllInstructorsAsync_DelegatesToCrudService()
    {
        var expected = new List<Instructor> { new() { Id = Guid.NewGuid() } };
        _mockCrudService.Setup(s => s.GetAllInstructorsAsync()).ReturnsAsync(expected);

        var result = await _service.GetAllInstructorsAsync();

        Assert.Same(expected, result);
        _mockCrudService.Verify(s => s.GetAllInstructorsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetInstructorByIdAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var expected = new Instructor { Id = id };
        _mockCrudService.Setup(s => s.GetInstructorByIdAsync(id)).ReturnsAsync(expected);

        var result = await _service.GetInstructorByIdAsync(id);

        Assert.Same(expected, result);
        _mockCrudService.Verify(s => s.GetInstructorByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task CreateInstructorAsync_DelegatesToCrudService()
    {
        var request = new CreateInstructor { Firstname = "John", Lastname = "Doe" };
        var expected = new Instructor { Id = Guid.NewGuid() };
        _mockCrudService.Setup(s => s.CreateInstructorAsync(request, _testUserPrincipal)).ReturnsAsync(expected);

        var result = await _service.CreateInstructorAsync(request, _testUserPrincipal);

        Assert.Same(expected, result);
        _mockCrudService.Verify(s => s.CreateInstructorAsync(request, _testUserPrincipal), Times.Once);
    }

    [Fact]
    public async Task UpdateInstructorAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var request = new UpdateInstructor { Firstname = "Jane" };
        var expected = new Instructor { Id = id };
        _mockCrudService.Setup(s => s.UpdateInstructorAsync(id, request, _testUserPrincipal)).ReturnsAsync(expected);

        var result = await _service.UpdateInstructorAsync(id, request, _testUserPrincipal);

        Assert.Same(expected, result);
        _mockCrudService.Verify(s => s.UpdateInstructorAsync(id, request, _testUserPrincipal), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteInstructorAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.SoftDeleteInstructorAsync(id, _testUserPrincipal)).Returns(Task.CompletedTask);

        await _service.SoftDeleteInstructorAsync(id, _testUserPrincipal);

        _mockCrudService.Verify(s => s.SoftDeleteInstructorAsync(id, _testUserPrincipal), Times.Once);
    }

    [Fact]
    public async Task HardDeleteInstructorAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.HardDeleteInstructorAsync(id, _testUserPrincipal)).Returns(Task.CompletedTask);

        await _service.HardDeleteInstructorAsync(id, _testUserPrincipal);

        _mockCrudService.Verify(s => s.HardDeleteInstructorAsync(id, _testUserPrincipal), Times.Once);
    }

    [Fact]
    public async Task RestoreInstructorAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.RestoreInstructorAsync(id, _testUserPrincipal)).Returns(Task.CompletedTask);

        await _service.RestoreInstructorAsync(id, _testUserPrincipal);

        _mockCrudService.Verify(s => s.RestoreInstructorAsync(id, _testUserPrincipal), Times.Once);
    }

    #endregion

    #region Query Delegation Tests

    [Fact]
    public async Task GetInstructorProfileAsync_DelegatesToQueryService()
    {
        var expected = new InstructorProfileResponseDto { Id = Guid.NewGuid() };
        _mockQueryService.Setup(s => s.GetInstructorProfileAsync(_testUserPrincipal)).ReturnsAsync(expected);

        var result = await _service.GetInstructorProfileAsync(_testUserPrincipal);

        Assert.Same(expected, result);
        _mockQueryService.Verify(s => s.GetInstructorProfileAsync(_testUserPrincipal), Times.Once);
    }

    [Fact]
    public async Task GetSubjectsByInstructorIdAsync_DelegatesToQueryService()
    {
        var id = Guid.NewGuid();
        var expected = new List<SubjectResponseDto> { new() { Id = Guid.NewGuid() } };
        _mockQueryService.Setup(s => s.GetSubjectsByInstructorIdAsync(id)).ReturnsAsync(expected);

        var result = await _service.GetSubjectsByInstructorIdAsync(id);

        Assert.Same(expected, result);
        _mockQueryService.Verify(s => s.GetSubjectsByInstructorIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task GetSchedulesByInstructorAsync_DelegatesToQueryService()
    {
        var expected = new List<ScheduleResponseDto> { new() };
        _mockQueryService.Setup(s => s.GetSchedulesByInstructorAsync(_testUserPrincipal)).ReturnsAsync(expected);

        var result = await _service.GetSchedulesByInstructorAsync(_testUserPrincipal);

        Assert.Same(expected, result);
        _mockQueryService.Verify(s => s.GetSchedulesByInstructorAsync(_testUserPrincipal), Times.Once);
    }

    [Fact]
    public async Task GetSectionsWithStudentsByInstructorAsync_DelegatesToQueryService()
    {
        var expected = new InstructorSectionsWithStudentsResponseDto { InstructorId = Guid.NewGuid() };
        _mockQueryService.Setup(s => s.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal)).ReturnsAsync(expected);

        var result = await _service.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal);

        Assert.Same(expected, result);
        _mockQueryService.Verify(s => s.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal), Times.Once);
    }

    #endregion

    #region Detail Delegation Tests

    [Fact]
    public async Task GetInstructorSectionsOverviewAsync_DelegatesToDetailService()
    {
        var expected = new List<InstructorSectionOverviewDto> { new() { SectionId = Guid.NewGuid() } };
        _mockDetailService.Setup(s => s.GetInstructorSectionsOverviewAsync(_testUserPrincipal)).ReturnsAsync(expected);

        var result = await _service.GetInstructorSectionsOverviewAsync(_testUserPrincipal);

        Assert.Same(expected, result);
        _mockDetailService.Verify(s => s.GetInstructorSectionsOverviewAsync(_testUserPrincipal), Times.Once);
    }

    [Fact]
    public async Task GetInstructorSectionDetailAsync_DelegatesToDetailService()
    {
        var sectionId = Guid.NewGuid();
        var expected = new InstructorSectionDetailDto { SectionId = sectionId };
        _mockDetailService.Setup(s => s.GetInstructorSectionDetailAsync(_testUserPrincipal, sectionId)).ReturnsAsync(expected);

        var result = await _service.GetInstructorSectionDetailAsync(_testUserPrincipal, sectionId);

        Assert.Same(expected, result);
        _mockDetailService.Verify(s => s.GetInstructorSectionDetailAsync(_testUserPrincipal, sectionId), Times.Once);
    }

    [Fact]
    public async Task GetInstructorSectionDetailByUuidAsync_DelegatesToDetailService()
    {
        var sectionUuid = Guid.NewGuid();
        var expected = new InstructorSectionDetailDto { SectionId = sectionUuid };
        _mockDetailService.Setup(s => s.GetInstructorSectionDetailByUuidAsync(_testUserPrincipal, sectionUuid)).ReturnsAsync(expected);

        var result = await _service.GetInstructorSectionDetailByUuidAsync(_testUserPrincipal, sectionUuid);

        Assert.Same(expected, result);
        _mockDetailService.Verify(s => s.GetInstructorSectionDetailByUuidAsync(_testUserPrincipal, sectionUuid), Times.Once);
    }

    [Fact]
    public async Task GetInstructorStudentDetailAsync_DelegatesToDetailService()
    {
        var studentId = Guid.NewGuid();
        var expected = new InstructorStudentDetailDto { StudentId = studentId };
        _mockDetailService.Setup(s => s.GetInstructorStudentDetailAsync(_testUserPrincipal, studentId)).ReturnsAsync(expected);

        var result = await _service.GetInstructorStudentDetailAsync(_testUserPrincipal, studentId);

        Assert.Same(expected, result);
        _mockDetailService.Verify(s => s.GetInstructorStudentDetailAsync(_testUserPrincipal, studentId), Times.Once);
    }

    [Fact]
    public async Task GetInstructorStudentDetailByUuidAsync_DelegatesToDetailService()
    {
        var studentUuid = Guid.NewGuid();
        var expected = new InstructorStudentDetailDto { StudentId = studentUuid };
        _mockDetailService.Setup(s => s.GetInstructorStudentDetailByUuidAsync(_testUserPrincipal, studentUuid)).ReturnsAsync(expected);

        var result = await _service.GetInstructorStudentDetailByUuidAsync(_testUserPrincipal, studentUuid);

        Assert.Same(expected, result);
        _mockDetailService.Verify(s => s.GetInstructorStudentDetailByUuidAsync(_testUserPrincipal, studentUuid), Times.Once);
    }

    #endregion

    #region Exception Propagation Tests

    [Fact]
    public async Task GetAllInstructorsAsync_PropagatesException()
    {
        _mockCrudService.Setup(s => s.GetAllInstructorsAsync())
            .ThrowsAsync(new EntityNotFoundException<Guid>("Instructor", Guid.NewGuid()));

        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.GetAllInstructorsAsync());
    }

    [Fact]
    public async Task GetInstructorProfileAsync_PropagatesException()
    {
        _mockQueryService.Setup(s => s.GetInstructorProfileAsync(_testUserPrincipal))
            .ThrowsAsync(new EntityServiceException("Instructor", "GetInstructorProfile", "error"));

        await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetInstructorProfileAsync(_testUserPrincipal));
    }

    #endregion
}
