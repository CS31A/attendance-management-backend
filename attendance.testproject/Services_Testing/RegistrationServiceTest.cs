using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Services.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace attendance.testproject.Services_Testing;

public class RegistrationServiceTest
{
    private readonly Mock<IAccountRepository> _accountRepository;
    private readonly Mock<ISectionRepository> _sectionRepository;
    private readonly Mock<IUserFactory> _userFactory;
    private readonly RegistrationService _service;

    public RegistrationServiceTest()
    {
        _accountRepository = new Mock<IAccountRepository>();
        _sectionRepository = new Mock<ISectionRepository>();
        _userFactory = new Mock<IUserFactory>();
        _service = new RegistrationService(
            _accountRepository.Object,
            _sectionRepository.Object,
            _userFactory.Object,
            NullLogger<RegistrationService>.Instance);
    }

    private RegisterDto CreateValidRegisterDto(string? role = "Student", Guid? sectionId = null)
    {
        return new RegisterDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Test@123",
            RepeatedPassword = "Test@123",
            Role = role,
            SectionId = sectionId,
            Firstname = "John",
            Lastname = "Doe"
        };
    }

    [Fact]
    public async Task RegisterAsync_ThrowsValidationException_WhenPasswordsDoNotMatch()
    {
        var registerDto = CreateValidRegisterDto(sectionId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
        registerDto.RepeatedPassword = "Different@123";

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(registerDto));

        Assert.Equal("Passwords do not match", exception.Message);
        _accountRepository.Verify(repo => repo.FindUserByUsernameAsync(It.IsAny<string>()), Times.Never);
        _accountRepository.Verify(repo => repo.FindUserByEmailAsync(It.IsAny<string>()), Times.Never);
        _userFactory.Verify(factory => factory.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsEntityAlreadyExistsException_WhenUsernameAlreadyExists()
    {
        var registerDto = CreateValidRegisterDto(sectionId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync("testuser")).ReturnsAsync(new IdentityUser { Id = "existing-id" });

        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => _service.RegisterAsync(registerDto));

        Assert.Equal("User", exception.EntityName);
        Assert.Equal("Username already exists", exception.EntityIdentifier);
        Assert.Equal("testuser", exception.IdentifierPropertyName);
        _accountRepository.Verify(repo => repo.FindUserByEmailAsync(It.IsAny<string>()), Times.Never);
        _userFactory.Verify(factory => factory.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsEntityAlreadyExistsException_WhenEmailAlreadyExists()
    {
        var registerDto = CreateValidRegisterDto(sectionId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync("testuser")).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync("test@example.com")).ReturnsAsync(new IdentityUser { Id = "existing-id" });

        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => _service.RegisterAsync(registerDto));

        Assert.Equal("User", exception.EntityName);
        Assert.Equal("Email already exists", exception.EntityIdentifier);
        Assert.Equal("test@example.com", exception.IdentifierPropertyName);
        _userFactory.Verify(factory => factory.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsValidationException_WhenRoleIsInvalid()
    {
        var registerDto = CreateValidRegisterDto(role: "Teacher", sectionId: Guid.Parse("11111111-1111-1111-1111-111111111111"));
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(registerDto));

        Assert.Equal("Invalid role specified. Valid roles are: Student, Instructor, Admin", exception.Message);
        _sectionRepository.Verify(repo => repo.GetSectionByUuidAsync(It.IsAny<Guid>()), Times.Never);
        _userFactory.Verify(factory => factory.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsValidationException_WhenNonStudentHasSectionId()
    {
        var registerDto = CreateValidRegisterDto(role: "Instructor", sectionId: Guid.NewGuid());
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(registerDto));

        Assert.Equal("SectionId should not be provided for Instructor role", exception.Message);
        _sectionRepository.Verify(repo => repo.GetSectionByUuidAsync(It.IsAny<Guid>()), Times.Never);
        _userFactory.Verify(factory => factory.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsValidationException_WhenStudentMissingSectionId()
    {
        var registerDto = CreateValidRegisterDto(role: "Student", sectionId: null);
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(registerDto));

        Assert.Equal("SectionId is required for student registration", exception.Message);
        _sectionRepository.Verify(repo => repo.GetSectionByUuidAsync(It.IsAny<Guid>()), Times.Never);
        _userFactory.Verify(factory => factory.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsEntityNotFoundException_WhenStudentSectionDoesNotExist()
    {
        var missingSectionId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var registerDto = CreateValidRegisterDto(role: "Student", sectionId: missingSectionId);
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _sectionRepository.Setup(repo => repo.GetSectionByUuidAsync(missingSectionId)).ReturnsAsync((Section?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.RegisterAsync(registerDto));

        Assert.Equal("Section", exception.EntityName);
        Assert.Equal(missingSectionId, exception.Key);
        Assert.Equal("The specified section does not exist", exception.Message);
        _userFactory.Verify(factory => factory.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsValidationException_WhenStudentMissingFirstname()
    {
        var sectionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var registerDto = CreateValidRegisterDto(role: "Student", sectionId: sectionId);
        registerDto.Firstname = null;
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _sectionRepository.Setup(repo => repo.GetSectionByUuidAsync(sectionId)).ReturnsAsync(new Section { Id = 1, Uuid = sectionId, Name = "Test Section" });

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(registerDto));

        Assert.Equal("Firstname is required for student registration", exception.Message);
        _userFactory.Verify(factory => factory.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsValidationException_WhenStudentMissingLastname()
    {
        var sectionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var registerDto = CreateValidRegisterDto(role: "Student", sectionId: sectionId);
        registerDto.Lastname = null;
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _sectionRepository.Setup(repo => repo.GetSectionByUuidAsync(sectionId)).ReturnsAsync(new Section { Id = 1, Uuid = sectionId, Name = "Test Section" });

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(registerDto));

        Assert.Equal("Lastname is required for student registration", exception.Message);
        _userFactory.Verify(factory => factory.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsSuccess_WhenStudentRegistrationIsValid()
    {
        var sectionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var registerDto = CreateValidRegisterDto(role: "Student", sectionId: sectionId);
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _sectionRepository.Setup(repo => repo.GetSectionByUuidAsync(sectionId)).ReturnsAsync(new Section { Id = 1, Uuid = sectionId, Name = "Test Section" });
        _userFactory.Setup(factory => factory.CreateUserAsync("testuser", "test@example.com", "Test@123", "Student", "John", "Doe", sectionId))
            .ReturnsAsync(new UserCreationResult { Success = true });

        var result = await _service.RegisterAsync(registerDto);

        Assert.True(result.Success);
        Assert.Equal("User registered successfully with Student role", result.Message);
        _userFactory.Verify(factory => factory.CreateUserAsync("testuser", "test@example.com", "Test@123", "Student", "John", "Doe", sectionId), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsSuccess_WhenInstructorRegistrationIsValid()
    {
        var registerDto = CreateValidRegisterDto(role: "Instructor", sectionId: null);
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _userFactory.Setup(factory => factory.CreateUserAsync("testuser", "test@example.com", "Test@123", "Instructor", "John", "Doe", null))
            .ReturnsAsync(new UserCreationResult { Success = true });

        var result = await _service.RegisterAsync(registerDto);

        Assert.True(result.Success);
        Assert.Equal("User registered successfully with Instructor role", result.Message);
        _userFactory.Verify(factory => factory.CreateUserAsync("testuser", "test@example.com", "Test@123", "Instructor", "John", "Doe", null), Times.Once);
        _sectionRepository.Verify(repo => repo.GetSectionByUuidAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsSuccess_WhenAdminRegistrationIsValid()
    {
        var registerDto = CreateValidRegisterDto(role: "Admin", sectionId: null);
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _userFactory.Setup(factory => factory.CreateUserAsync("testuser", "test@example.com", "Test@123", "Admin", "John", "Doe", null))
            .ReturnsAsync(new UserCreationResult { Success = true });

        var result = await _service.RegisterAsync(registerDto);

        Assert.True(result.Success);
        Assert.Equal("User registered successfully with Admin role", result.Message);
        _userFactory.Verify(factory => factory.CreateUserAsync("testuser", "test@example.com", "Test@123", "Admin", "John", "Doe", null), Times.Once);
        _sectionRepository.Verify(repo => repo.GetSectionByUuidAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_TreatsNullRoleAsStudent()
    {
        var sectionId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var registerDto = CreateValidRegisterDto(role: null, sectionId: sectionId);
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _sectionRepository.Setup(repo => repo.GetSectionByUuidAsync(sectionId)).ReturnsAsync(new Section { Id = 1, Uuid = sectionId, Name = "Test Section" });
        _userFactory.Setup(factory => factory.CreateUserAsync("testuser", "test@example.com", "Test@123", "Student", "John", "Doe", sectionId))
            .ReturnsAsync(new UserCreationResult { Success = true });

        var result = await _service.RegisterAsync(registerDto);

        Assert.True(result.Success);
        Assert.Equal("User registered successfully with Student role", result.Message);
        _userFactory.Verify(factory => factory.CreateUserAsync("testuser", "test@example.com", "Test@123", "Student", "John", "Doe", sectionId), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsEntityServiceException_WhenFactoryReturnsFailure()
    {
        var registerDto = CreateValidRegisterDto(role: "Instructor", sectionId: null);
        _accountRepository.Setup(repo => repo.FindUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _accountRepository.Setup(repo => repo.FindUserByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser?)null);
        _userFactory.Setup(factory => factory.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new UserCreationResult { Success = false, Errors = new[] { "Error 1", "Error 2" } });

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.RegisterAsync(registerDto));

        Assert.Equal("User", exception.EntityName);
        Assert.Equal("registration", exception.Operation);
        Assert.Contains("Error 1", exception.Message);
        Assert.Contains("Error 2", exception.Message);
    }
}
