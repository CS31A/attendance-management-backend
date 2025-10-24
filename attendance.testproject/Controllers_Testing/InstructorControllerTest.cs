using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Controllers;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace attendance.testproject.Controllers_Testing;

public class InstructorControllerTest
{
    private readonly Mock<IInstructorService> _service;
    private readonly Mock<ILogger<InstructorController>> _logger;
    private readonly InstructorController _controller;
    private readonly ClaimsPrincipal _mockUser;

    public InstructorControllerTest()
    {
        _service = new Mock<IInstructorService>();
        _logger = new Mock<ILogger<InstructorController>>();
        _controller = new InstructorController(_service.Object, _logger.Object);

        _mockUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _mockUser }
        };
    }

    [Fact]
    public async Task GetInstructors_ReturnsOkWithInstructors()
    {
        var instructors = new List<Instructor>
        {
            new() { Id = 1, Firstname = "John", Lastname = "Doe", Email = "john@test.com", UserId = "user1" },
            new() { Id = 2, Firstname = "Jane", Lastname = "Smith", Email = "jane@test.com", UserId = "user2" }
        };
        _service.Setup(s => s.GetAllInstructorsAsync()).ReturnsAsync(instructors);

        var result = await _controller.GetInstructors();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsAssignableFrom<IEnumerable<Instructor>>(okResult.Value);
        Assert.Equal(2, returnValue.Count());
    }

    [Fact]
    public async Task GetInstructors_ReturnsServerErrorOnException()
    {
        _service.Setup(s => s.GetAllInstructorsAsync())
            .ThrowsAsync(new EntityServiceException("Instructor", "GetAll", "Database error"));

        var result = await _controller.GetInstructors();

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetInstructor_ReturnsOkWithInstructor()
    {
        var instructor = new Instructor
        {
            Id = 1,
            Firstname = "John",
            Lastname = "Doe",
            Email = "john@test.com",
            UserId = "user1"
        };
        _service.Setup(s => s.GetInstructorByIdAsync(1)).ReturnsAsync(instructor);

        var result = await _controller.GetInstructor(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<Instructor>(okResult.Value);
        Assert.Equal(1, returnValue.Id);
        Assert.Equal("John", returnValue.Firstname);
    }

    [Fact]
    public async Task GetInstructor_ReturnsNotFoundWhenDoesNotExist()
    {
        _service.Setup(s => s.GetInstructorByIdAsync(999))
            .ThrowsAsync(new EntityNotFoundException<int>("Instructor", 999));

        var result = await _controller.GetInstructor(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetInstructorSubjects_ReturnsOkWithSubjects()
    {
        var subjects = new List<SubjectResponseDto>
        {
            new() { Id = 1, Name = "Mathematics", Code = "MATH101" },
            new() { Id = 2, Name = "Science", Code = "SCI101" }
        };
        _service.Setup(s => s.GetSubjectsByInstructorIdAsync(1)).ReturnsAsync(subjects);

        var result = await _controller.GetInstructorSubjects(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsAssignableFrom<IEnumerable<SubjectResponseDto>>(okResult.Value);
        Assert.Equal(2, returnValue.Count());
    }

    [Fact]
    public async Task GetInstructorSubjects_ReturnsNotFoundWhenInstructorDoesNotExist()
    {
        _service.Setup(s => s.GetSubjectsByInstructorIdAsync(999))
            .ThrowsAsync(new EntityNotFoundException<int>("Instructor", 999));

        var result = await _controller.GetInstructorSubjects(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetInstructorProfile_ReturnsOkWithProfile()
    {
        var profile = new InstructorProfileResponseDto
        {
            Id = 1,
            Firstname = "John",
            Lastname = "Doe",
            Email = "john@test.com"
        };
        _service.Setup(s => s.GetInstructorProfileAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(profile);

        var result = await _controller.GetInstructorProfile();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<InstructorProfileResponseDto>(okResult.Value);
        Assert.Equal(1, returnValue.Id);
    }

    [Fact]
    public async Task GetInstructorProfile_ReturnsNotFoundWhenProfileDoesNotExist()
    {
        _service.Setup(s => s.GetInstructorProfileAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((InstructorProfileResponseDto?)null);

        var result = await _controller.GetInstructorProfile();

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task PatchInstructor_ReturnsOkWithUpdatedInstructor()
    {
        var updateDto = new UpdateInstructor { Firstname = "UpdatedName" };
        var instructor = new Instructor
        {
            Id = 1,
            Firstname = "UpdatedName",
            Lastname = "Doe",
            UserId = "user1"
        };
        _service.Setup(s => s.UpdateInstructorAsync(1, updateDto, It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(instructor);

        var result = await _controller.PatchInstructor(1, updateDto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<Instructor>(okResult.Value);
        Assert.Equal("UpdatedName", returnValue.Firstname);
    }

    [Fact]
    public async Task PatchInstructor_ReturnsNotFoundWhenInstructorDoesNotExist()
    {
        var updateDto = new UpdateInstructor { Firstname = "Test" };
        _service.Setup(s => s.UpdateInstructorAsync(999, updateDto, It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new EntityNotFoundException<int>("Instructor", 999));

        var result = await _controller.PatchInstructor(999, updateDto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task PatchInstructor_ReturnsUnauthorizedWhenNotAuthorized()
    {
        var updateDto = new UpdateInstructor { Firstname = "Test" };
        _service.Setup(s => s.UpdateInstructorAsync(1, updateDto, It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new EntityUnauthorizedException("Instructor", "Update", "user123"));

        var result = await _controller.PatchInstructor(1, updateDto);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task SoftDeleteInstructor_ReturnsOkOnSuccess()
    {
        _service.Setup(s => s.SoftDeleteInstructorAsync(1, It.IsAny<ClaimsPrincipal>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.SoftDeleteInstructor(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SoftDeleteResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Instructor marked as deleted successfully", response.Message);
    }

    [Fact]
    public async Task SoftDeleteInstructor_ReturnsNotFoundWhenInstructorDoesNotExist()
    {
        _service.Setup(s => s.SoftDeleteInstructorAsync(999, It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new EntityNotFoundException<int>("Instructor", 999));

        var result = await _controller.SoftDeleteInstructor(999);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<SoftDeleteResponse>(notFoundResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task SoftDeleteInstructor_ReturnsUnauthorizedWhenNotAuthorized()
    {
        _service.Setup(s => s.SoftDeleteInstructorAsync(1, It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new EntityUnauthorizedException("Instructor", "SoftDelete", "user123"));

        var result = await _controller.SoftDeleteInstructor(1);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<SoftDeleteResponse>(unauthorizedResult.Value);
        Assert.False(response.Success);
    }

    //[Fact]
    //public async Task HardDeleteInstructor_ReturnsOkOnSuccess()
    //{
    //    _service.Setup(s => s.HardDeleteInstructorAsync(1, It.IsAny<ClaimsPrincipal>()))
    //        .ReturnsAsync((string?)null);

    //    var result = await _controller.HardDeleteInstructor(1);

    //    var okResult = Assert.IsType<OkObjectResult>(result.Result);
    //    var response = Assert.IsType<SoftDeleteResponse>(okResult.Value);
    //    Assert.True(response.Success);
    //    Assert.Equal("Instructor permanently deleted successfully", response.Message);
    //}

    //[Fact]
    //public async Task HardDeleteInstructor_ReturnsNotFoundWhenErrorContainsNotFound()
    //{
    //    _service.Setup(s => s.HardDeleteInstructorAsync(999, It.IsAny<ClaimsPrincipal>()))
    //        .ReturnsAsync("Instructor not found");

    //    var result = await _controller.HardDeleteInstructor(999);

    //    var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
    //    var response = Assert.IsType<SoftDeleteResponse>(notFoundResult.Value);
    //    Assert.False(response.Success);
    //}

    [Fact]
    public async Task RestoreInstructor_ReturnsOkOnSuccess()
    {
        _service.Setup(s => s.RestoreInstructorAsync(1, It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((string?)null);

        var result = await _controller.RestoreInstructor(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SoftDeleteResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Instructor restored successfully", response.Message);
    }

    [Fact]
    public async Task RestoreInstructor_ReturnsBadRequestWhenErrorOccurs()
    {
        _service.Setup(s => s.RestoreInstructorAsync(1, It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync("Instructor is already active");

        var result = await _controller.RestoreInstructor(1);

        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<SoftDeleteResponse>(badResult.Value);
        Assert.False(response.Success);
    }
}