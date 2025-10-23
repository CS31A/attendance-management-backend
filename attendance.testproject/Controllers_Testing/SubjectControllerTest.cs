using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.Classes;
using attendance_monitoring.Controllers;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace attendance.testproject.Controllers_Testing;

public class SubjectControllerTest
{
    private readonly Mock<ISubjectService> _service;
    private readonly Mock<ILogger<SubjectController>> _logger;
    private readonly SubjectController _controller;

    public SubjectControllerTest()
    {
        _service = new Mock<ISubjectService>();
        _logger = new Mock<ILogger<SubjectController>>();
        _controller = new SubjectController(_service.Object, _logger.Object);
    }

    [Fact]
    public async Task GetSubjects_ReturnsOkWithSubjects()
    {
        var subjects = new List<Subject>
        {
            new() { Id = 1, Name = "Mathematics", Code = "MATH101", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Science", Code = "SCI101", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        _service.Setup(s => s.GetAllSubjectsAsync()).ReturnsAsync(subjects);

        var result = await _controller.GetSubjects();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsAssignableFrom<IEnumerable<Subject>>(okResult.Value);
        Assert.Equal(2, returnValue.Count());
    }

    [Fact]
    public async Task GetSubjects_ReturnsServerErrorOnEntityServiceException()
    {
        _service.Setup(s => s.GetAllSubjectsAsync())
            .ThrowsAsync(new EntityServiceException("Subject", "GetAll", "Database error"));

        var result = await _controller.GetSubjects();

        var objResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objResult.StatusCode);
    }

    [Fact]
    public async Task GetSubjects_ReturnsServerErrorOnGeneralException()
    {
        // Arrange
        _mockSubjectService
            .Setup(s => s.GetAllSubjectsAsync())
            .ThrowsAsync(new SubjectServiceException("GetAllSubjects", "Service error"));

        var result = await _controller.GetSubjects();

        var objResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objResult.StatusCode);
    }

    [Fact]
    public async Task GetSubject_ReturnsOkWithSubject()
    {
        var subject = new Subject
        {
            Id = 1,
            Name = "Mathematics",
            Code = "MATH101",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _service.Setup(s => s.GetSubjectByIdAsync(1)).ReturnsAsync(subject);

        var result = await _controller.GetSubject(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<Subject>(okResult.Value);
        Assert.Equal(1, returnValue.Id);
        Assert.Equal("Mathematics", returnValue.Name);
    }

    [Fact]
    public async Task GetSubject_ReturnsNotFoundWhenDoesNotExist()
    {
        _service.Setup(s => s.GetSubjectByIdAsync(999))
            .ThrowsAsync(new EntityNotFoundException<int>("Subject", 999));

        var result = await _controller.GetSubject(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateSubject_ReturnsCreatedWithSubject()
    {
        var createDto = new CreateSubject { Name = "Physics", Code = "PHY101" };
        var subject = new Subject
        {
            Id = 1,
            Name = "Physics",
            Code = "PHY101",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _service.Setup(s => s.CreateSubjectAsync(createDto))
            .ReturnsAsync((subject, null as string));

        var result = await _controller.CreateSubject(createDto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnValue = Assert.IsType<Subject>(createdResult.Value);
        Assert.Equal("Physics", returnValue.Name);
        Assert.Equal("PHY101", returnValue.Code);
    }

    [Fact]
    public async Task CreateSubject_ReturnsBadRequestOnError()
    {
        var createDto = new CreateSubject { Name = "Physics", Code = "PHY101" };
        _service.Setup(s => s.CreateSubjectAsync(createDto))
            .ReturnsAsync((null as Subject, "A subject with this code already exists"));

        var result = await _controller.CreateSubject(createDto);

        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("A subject with this code already exists", badResult.Value);
    }

    [Fact]
    public async Task CreateSubject_ReturnsBadRequestWhenSubjectIsNull()
    {
        var createDto = new CreateSubject { Name = "Physics", Code = "PHY101" };
        _service.Setup(s => s.CreateSubjectAsync(createDto))
            .ReturnsAsync((null as Subject, null as string));

        var result = await _controller.CreateSubject(createDto);

        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("An unexpected error occurred while creating the subject.", badResult.Value);
    }

    [Fact]
    public async Task UpdateSubject_ReturnsOkWithUpdatedSubject()
    {
        var updateDto = new UpdateSubject { Name = "Advanced Math", Code = "MATH201" };
        var subject = new Subject
        {
            Id = 1,
            Name = "Advanced Math",
            Code = "MATH201",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _service.Setup(s => s.UpdateSubjectAsync(1, updateDto))
            .ReturnsAsync((subject, null as string));

        var result = await _controller.UpdateSubject(1, updateDto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<Subject>(okResult.Value);
        Assert.Equal("Advanced Math", returnValue.Name);
        Assert.Equal("MATH201", returnValue.Code);
    }

    [Fact]
    public async Task UpdateSubject_ReturnsNotFoundWhenDoesNotExist()
    {
        var updateDto = new UpdateSubject { Name = "Updated" };
        _service.Setup(s => s.UpdateSubjectAsync(999, updateDto))
            .ThrowsAsync(new EntityNotFoundException<int>("Subject", 999));

        var result = await _controller.UpdateSubject(999, updateDto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateSubject_ReturnsBadRequestOnError()
    {
        var updateDto = new UpdateSubject { Code = "MATH101" };
        _service.Setup(s => s.UpdateSubjectAsync(1, updateDto))
            .ReturnsAsync((null as Subject, "A subject with this code already exists"));

        var result = await _controller.UpdateSubject(1, updateDto);

        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("A subject with this code already exists", badResult.Value);
    }

    [Fact]
    public async Task DeleteSubject_ReturnsNoContentOnSuccess()
    {
        _service.Setup(s => s.DeleteSubjectAsync(1))
            .ReturnsAsync(null as string);

        var result = await _controller.DeleteSubject(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteSubject_ReturnsNotFoundWhenDoesNotExist()
    {
        _service.Setup(s => s.DeleteSubjectAsync(999))
            .ThrowsAsync(new EntityNotFoundException<int>("Subject", 999));

        var result = await _controller.DeleteSubject(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteSubject_ReturnsBadRequestOnError()
    {
        _service.Setup(s => s.DeleteSubjectAsync(1))
            .ReturnsAsync("Cannot delete subject with active schedules");

        var result = await _controller.DeleteSubject(1);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Cannot delete subject with active schedules", badResult.Value);
    }
}