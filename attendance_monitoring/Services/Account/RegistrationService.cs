using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services.Account;

/// <summary>
/// Focused unit responsible for user registration operations.
/// Handles validation, role assignment, and user creation.
/// </summary>
internal sealed class RegistrationService : IRegistrationService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IUserFactory _userFactory;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        IAccountRepository accountRepository,
        ISectionRepository sectionRepository,
        IUserFactory userFactory,
        ILogger<RegistrationService> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _userFactory = userFactory ?? throw new ArgumentNullException(nameof(userFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="registerDto">The registration data.</param>
    /// <returns>The registration response.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    /// <exception cref="EntityAlreadyExistsException{String}">Thrown when username or email already exists.</exception>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when the specified section does not exist.</exception>
    /// <exception cref="EntityServiceException">Thrown when user creation fails.</exception>
    public async Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        _logger.LogInformation("Registration attempt for username: {Username}", registerDto.Username);

        if (registerDto.Password != registerDto.RepeatedPassword)
        {
            _logger.LogWarning("Registration failed for username {Username}: Passwords do not match", registerDto.Username);
            throw new ValidationException("Passwords do not match");
        }

        var existingUser = await _accountRepository.FindUserByUsernameAsync(registerDto.Username).ConfigureAwait(false);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed for username {Username}: Username already exists", registerDto.Username);
            throw new EntityAlreadyExistsException<string>("User", registerDto.Username, "Username already exists");
        }

        existingUser = await _accountRepository.FindUserByEmailAsync(registerDto.Email).ConfigureAwait(false);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration failed for username {Username}: Email already exists", registerDto.Username);
            throw new EntityAlreadyExistsException<string>("User", registerDto.Email, "Email already exists");
        }

        var validRoles = new[] { RoleConstants.Admin, RoleConstants.Instructor, RoleConstants.Student };
        // Role assignment logic (roles are now ensured to exist at application startup)
        var roleToAssign = RoleConstants.Student;
        if (!string.IsNullOrEmpty(registerDto.Role))
        {
            if (!validRoles.Contains(registerDto.Role, StringComparer.OrdinalIgnoreCase))
            {
                throw new ValidationException("Invalid role specified. Valid roles are: Student, Instructor, Admin");
            }

            roleToAssign = registerDto.Role;
        }

        // Defensive validation: Non-students should not have a SectionId
        if (!roleToAssign.Equals("Student", StringComparison.OrdinalIgnoreCase) && registerDto.SectionId.HasValue)
        {
            _logger.LogWarning("Registration blocked for username {Username}: SectionId provided for non-student role {Role}",
                registerDto.Username, roleToAssign);
            throw new ValidationException($"SectionId should not be provided for {roleToAssign} role");
        }

        // For students, validate that the SectionId exists before attempting user creation
        if (roleToAssign.Equals("Student", StringComparison.OrdinalIgnoreCase))
        {
            // Check if SectionId is provided for students
            if (!registerDto.SectionId.HasValue)
            {
                _logger.LogWarning("Student registration failed for username {Username}: SectionId is required for students", registerDto.Username);
                throw new ValidationException("SectionId is required for student registration");
            }

            Section? section;
            try
            {
                section = await _sectionRepository.GetSectionByIdAsync(registerDto.SectionId.Value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Student registration failed for username {Username}: Section lookup failed for SectionId {SectionId}",
                    registerDto.Username,
                    registerDto.SectionId.Value);
                throw;
            }

            if (section == null)
            {
                _logger.LogWarning("Student registration failed for username {Username}: SectionId {SectionId} does not exist", registerDto.Username, registerDto.SectionId);
                throw new EntityNotFoundException<int>("Section", registerDto.SectionId.Value, "The specified section does not exist");
            }

            // Validate that Firstname is provided for students
            if (string.IsNullOrWhiteSpace(registerDto.Firstname))
            {
                _logger.LogWarning("Student registration failed for username {Username}: Firstname is required", registerDto.Username);
                throw new ValidationException("Firstname is required for student registration");
            }

            // Validate that Lastname is provided for students
            if (string.IsNullOrWhiteSpace(registerDto.Lastname))
            {
                _logger.LogWarning("Student registration failed for username {Username}: Lastname is required", registerDto.Username);
                throw new ValidationException("Lastname is required for student registration");
            }
        }

        // Use UserFactory to create the user with appropriate role and profile
        var userCreationResult = await _userFactory.CreateUserAsync(
            registerDto.Username,
            registerDto.Email,
            registerDto.Password,
            roleToAssign,
            registerDto.Firstname,
            registerDto.Lastname,
            roleToAssign.Equals("Student", StringComparison.OrdinalIgnoreCase) ? registerDto.SectionId : null
        ).ConfigureAwait(false);

        if (!userCreationResult.Success)
        {
            var errors = string.Join("; ", userCreationResult.Errors);
            _logger.LogWarning("User registration failed for username {Username}: {Errors}", registerDto.Username, errors);
            throw new EntityServiceException("User", "registration", errors);
        }

        _logger.LogInformation("User registered successfully: {Username} with role {Role}", registerDto.Username, roleToAssign);
        return new RegisterResponseDto { Success = true, Message = $"User registered successfully with {roleToAssign} role" };
    }
}
