# Testing Guide

This guide covers testing strategies and practices for the Attendance Management System.

## Test Project Structure

```
attendance.testproject/
├── Controllers_Testing/     # Controller unit tests
│   ├── AccountControllerTest.cs
│   ├── StudentControllerTest.cs
│   ├── InstructorControllerTest.cs
│   ├── SessionControllerTest.cs
│   ├── SubjectControllerTest.cs
│   ├── ScheduleControllerTest.cs
│   ├── QrCodeControllerTest.cs
│   ├── UserControllerTest.cs
│   ├── StudentEnrollmentControllerTest.cs
│   └── HealthCheckTest.cs
├── Services_Testing/        # Service layer tests
│   ├── SessionServiceTest.cs
│   ├── StudentEnrollmentServiceTest.cs
│   ├── AttendanceConcurrencyTests.cs
│   ├── AttendanceAuthorizationTests.cs
│   ├── JwtConfigurationValidatorTest.cs
│   └── OrphanedUserCleanupServiceTests.cs
├── Repositories_Testing/    # Repository tests
│   └── AttendanceRepositoryTest.cs
├── Integration_Testing/     # Limited integration tests (database constraints)
│   └── DatabaseConstraintIntegrationTests.cs
├── Database_Testing/        # Database-related tests
│   └── OrphanedUserConstraintTests.cs
└── TestResults/             # Test output and coverage reports
```

Note: The project currently does not have true end-to-end (E2E) tests or comprehensive integration tests. The Integration_Testing directory contains database constraint tests that verify specific database behaviors but don't test the full application stack.

## Running Tests

### All Tests
```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run using Makefile
make test
```

### With Code Coverage
```bash
# Using XPlat Code Coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report (requires ReportGenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

### Specific Test Project
```bash
dotnet test attendance.testproject/
```

### Run Specific Tests
```bash
# By test name
dotnet test --filter "FullyQualifiedName~StudentControllerTest"

# By category
dotnet test --filter "Category=Unit"

# Using Makefile to run a specific test class
make test-specific
```

## Writing Unit Tests

### Controller Tests

```csharp
public class StudentControllerTest
{
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly StudentController _studentController;

    public StudentControllerTest()
    {
        _mockStudentService = new Mock<IStudentService>();
        var mockLogger = new Mock<ILogger<StudentController>>();
        _studentController = new StudentController(_mockStudentService.Object, mockLogger.Object);
    }

    [Fact]
    public async Task GetStudents_ReturnsOkResult_WithStudentsList()
    {
        // Arrange
        var expectedStudents = new List<Student>
        {
            new Student { Id = 1, Firstname = "John", Lastname = "Doe" },
            new Student { Id = 2, Firstname = "Jane", Lastname = "Smith" }
        };

        _mockStudentService
            .Setup(s => s.GetAllNonDeletedStudentsAsync())
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _studentController.GetStudents();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IList<Student>>(okResult.Value);
        Assert.Equal(2, students.Count);
        Assert.Equal("John", students.First().Firstname);
        
        _mockStudentService.Verify(s => s.GetAllNonDeletedStudentsAsync(), Times.Once);
    }

    [Fact]
    public async Task SearchStudentsByName_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var searchTerm = "john";
        var expectedStudents = new List<Student>
        {
            new Student { Id = 1, Firstname = "John", Lastname = "Doe", UserId = "user1" },
            new Student { Id = 2, Firstname = "Johnny", Lastname = "Smith", UserId = "user2" }
        };

        _mockStudentService
            .Setup(s => s.SearchStudentsByNameAsync(searchTerm, 1, 50))
            .ReturnsAsync(expectedStudents);

        // Act
        var result = await _studentController.SearchStudentsByName(searchTerm);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var students = Assert.IsAssignableFrom<IEnumerable<Student>>(okResult.Value);
        Assert.Equal(2, students.Count());
        _mockStudentService.Verify(s => s.SearchStudentsByNameAsync(searchTerm, 1, 50), Times.Once);
    }

    [Fact]
    public async Task SearchStudentsByName_WithNullQuery_ReturnsBadRequest()
    {
        // Act
        var result = await _studentController.SearchStudentsByName(null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Query parameter is required", badRequestResult.Value);
        _mockStudentService.Verify(s => s.SearchStudentsByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}
```

### Service Tests

```csharp
public class StudentEnrollmentServiceTest
{
    private readonly Mock<IStudentEnrollmentRepository> _mockEnrollmentRepo;
    private readonly Mock<IStudentRepository> _mockStudentRepo;
    private readonly Mock<ISectionRepository> _mockSectionRepo;
    private readonly Mock<ISubjectRepository> _mockSubjectRepo;
    private readonly StudentEnrollmentService _service;

    public StudentEnrollmentServiceTest()
    {
        _mockEnrollmentRepo = new Mock<IStudentEnrollmentRepository>();
        _mockStudentRepo = new Mock<IStudentRepository>();
        _mockSectionRepo = new Mock<ISectionRepository>();
        _mockSubjectRepo = new Mock<ISubjectRepository>();

        _service = new StudentEnrollmentService(
            _mockEnrollmentRepo.Object,
            _mockStudentRepo.Object,
            _mockSectionRepo.Object,
            _mockSubjectRepo.Object
        );
    }

    [Fact]
    public async Task EnrollStudentAsync_ValidData_CreatesNewEnrollment()
    {
        // Arrange
        var studentId = 1;
        var sectionId = 2;
        var subjectId = 3;
        var student = new Student { Id = studentId, SectionId = 5, IsDeleted = false };
        var section = new Section { Id = sectionId };
        var subject = new Subject { Id = subjectId };
        var expectedEnrollment = new StudentEnrollment
        {
            Id = 1,
            StudentId = studentId,
            SectionId = sectionId,
            SubjectId = subjectId,
            IsActive = true,
            EnrollmentType = "Irregular"
        };

        _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId)).ReturnsAsync(student);
        _mockSectionRepo.Setup(r => r.GetSectionByIdAsync(sectionId)).ReturnsAsync(section);
        _mockSubjectRepo.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(subject);
        _mockEnrollmentRepo.Setup(r => r.GetEnrollmentAsync(studentId, sectionId, subjectId))
            .ReturnsAsync((StudentEnrollment?)null);
        _mockEnrollmentRepo.Setup(r => r.CreateAsync(It.IsAny<StudentEnrollment>()))
            .ReturnsAsync(expectedEnrollment);

        // Act
        var result = await _service.EnrollStudentAsync(studentId, sectionId, subjectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(studentId, result.StudentId);
        Assert.Equal(sectionId, result.SectionId);
        Assert.Equal(subjectId, result.SubjectId);
        Assert.True(result.IsActive);
        _mockEnrollmentRepo.Verify(r => r.CreateAsync(It.IsAny<StudentEnrollment>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullDependency_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StudentEnrollmentService(null!, _mockStudentRepo.Object, _mockSectionRepo.Object, _mockSubjectRepo.Object));
        Assert.Throws<ArgumentNullException>(() => new StudentEnrollmentService(_mockEnrollmentRepo.Object, null!, _mockSectionRepo.Object, _mockSubjectRepo.Object));
        Assert.Throws<ArgumentNullException>(() => new StudentEnrollmentService(_mockEnrollmentRepo.Object, _mockStudentRepo.Object, null!, _mockSubjectRepo.Object));
        Assert.Throws<ArgumentNullException>(() => new StudentEnrollmentService(_mockEnrollmentRepo.Object, _mockStudentRepo.Object, _mockSectionRepo.Object, null!));
    }
}
```

## Limited Integration Testing

### Database Constraint Tests

The project has limited integration tests that focus on verifying database constraints and business logic. These are not true end-to-end tests but rather unit tests that work with an in-memory database.

```csharp
/// <summary>
/// Integration tests for database constraint scenarios.
/// Tests the end-to-end behavior of user creation with profile creation
/// to ensure orphaned user prevention works correctly.
/// </summary>
public class DatabaseConstraintIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly Mock<ILogger<UserFactory>> _mockLogger;

    public DatabaseConstraintIntegrationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup mocks
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockLogger = new Mock<ILogger<UserFactory>>();
    }

    [Fact]
    public async Task CreateUser_WithProfile_ShouldPreventOrphanedUsers()
    {
        // Arrange
        var userId = "test-user-id";
        var email = "test@example.com";
        var userFactory = new UserFactory(_mockLogger.Object, _mockAccountRepository.Object);
        
        // Act
        var result = await userFactory.CreateStudentUserAsync(userId, email, "John", "Doe", 1);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.User);
        Assert.NotNull(result.Student);
        Assert.Equal(userId, result.Student.UserId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

Note: The project currently lacks true end-to-end tests that would verify:
- API endpoints with actual HTTP requests/responses
- Full integration between controllers, services, and repositories
- Authentication and authorization flows
- Real database interactions (SQL Server instead of in-memory)

To implement comprehensive integration testing, consider adding:
- TestServer with WebApplicationFactory
- Integration test project with real database
- TestContainers for containerized database testing
- API contract tests

### Repository Database Tests

```csharp
public class AttendanceRepositoryTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AttendanceRepository _repository;

    public AttendanceRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new AttendanceRepository(_context);
    }

    [Fact]
    public async Task GetAllForListingOptimizedAsync_ShouldReturnCorrectData()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetAllForListingOptimizedAsync(1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        Assert.All(result, item =>
        {
            Assert.True(item.Id > 0);
            Assert.False(string.IsNullOrEmpty(item.StudentName));
            Assert.False(string.IsNullOrEmpty(item.SubjectName));
            Assert.False(string.IsNullOrEmpty(item.Status));
        });
    }

    private async Task SeedTestDataAsync()
    {
        // Add test data to the in-memory database
        var student = new Student { Firstname = "Test", Lastname = "Student" };
        var subject = new Subject { Name = "Test Subject" };
        var attendance = new Attendance 
        { 
            StudentId = student.Id, 
            SubjectId = subject.Id, 
            Status = "Present" 
        };

        await _context.Students.AddAsync(student);
        await _context.Subjects.AddAsync(subject);
        await _context.Attendances.AddAsync(attendance);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

## Mocking Patterns

### Mocking DbContext
```csharp
private static DbContextOptions<ApplicationDbContext> CreateInMemoryOptions()
{
    return new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options;
}

[Fact]
public async Task Repository_AddsEntity()
{
    using var context = new ApplicationDbContext(CreateInMemoryOptions());
    var repository = new StudentRepository(context);

    var student = new Student { Firstname = "Test" };
    await repository.CreateStudent(student);
    await context.SaveChangesAsync();

    Assert.Equal(1, context.Students.Count());
}

// Example from actual AttendanceRepositoryTest
public class AttendanceRepositoryTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AttendanceRepository _repository;

    public AttendanceRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new AttendanceRepository(_context);
    }
}
```

### Mocking HttpContext
```csharp
private static ControllerContext CreateControllerContext(ClaimsPrincipal user)
{
    return new ControllerContext
    {
        HttpContext = new DefaultHttpContext { User = user }
    };
}

[Fact]
public async Task GetCurrentUser_ReturnsUser()
{
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
    var identity = new ClaimsIdentity(claims, "Test");
    var principal = new ClaimsPrincipal(identity);

    _controller.ControllerContext = CreateControllerContext(principal);
    // ...
}
```

### Mocking UserContextService
```csharp
// Mock UserManager for UserContextService
var mockUserStore = new Mock<IUserStore<IdentityUser>>();
var mockUserManager = new Mock<UserManager<IdentityUser>>(
    mockUserStore.Object, null, null, null, null, null, null, null, null);

// Mock HttpContextAccessor
var mockContext = new Mock<IHttpContextAccessor>();
mockContext.Setup(c => c.HttpContext).Returns(new DefaultHttpContext());

// Create real UserContextService with mocked UserManager and context
_userContextService = new UserContextService(mockUserManager.Object, mockContext.Object);

// In tests that use UserContextService
[Fact]
public async Task Method_WithCurrentUser_PerformsAction()
{
    // Arrange
    var userId = "test-user-id";
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
    var identity = new ClaimsIdentity(claims, "Test");
    var principal = new ClaimsPrincipal(identity);
    
    mockHttpContext.Setup(c => c.HttpContext.User).Returns(principal);
    mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
        .Returns(userId);
}
```

### Mocking External Services
```csharp
_mockTokenService.Setup(s => s.GenerateAccessToken(It.IsAny<IdentityUser>()))
    .Returns("mock-jwt-token");

_mockTokenService.Setup(s => s.GenerateRefreshToken())
    .Returns(new RefreshToken { Token = "mock-refresh-token" });
```

## Test Categories

While traits can be used to categorize tests, this project typically organizes tests by directory structure:

```
Controllers_Testing/    # API controller tests
Services_Testing/       # Business logic tests  
Repositories_Testing/   # Data access tests
Integration_Testing/    # Limited integration tests (database constraints only)
Database_Testing/       # Database constraint tests
```

The project currently focuses on unit testing with in-memory databases. True end-to-end testing that tests the full application stack is not implemented at this time.

If you need to add traits, you can use:
```csharp
[Fact]
[Trait("Category", "Unit")]
public async Task UnitTest_Example() { }

[Fact]
[Trait("Category", "Integration")]
public async Task IntegrationTest_Example() { }
```

Run by category:
```bash
dotnet test --filter "Category=Unit"
```

## Best Practices

### Naming Conventions
```
MethodName_StateUnderTest_ExpectedBehavior
```
Examples:
- `GetStudent_ValidId_ReturnsStudent`
- `CreateStudent_DuplicateEmail_ThrowsConflictException`
- `Login_InvalidPassword_ReturnsUnauthorized`

### Arrange-Act-Assert Pattern
```csharp
[Fact]
public async Task Example()
{
    // Arrange - Set up test data and mocks
    var input = new TestInput();

    // Act - Execute the method under test
    var result = await _service.MethodAsync(input);

    // Assert - Verify the results
    Assert.NotNull(result);
}
```

### Test Isolation
- Each test should be independent
- Use fresh mocks for each test
- Avoid shared state between tests

### Exception Testing
```csharp
[Fact]
public async Task Method_InvalidInput_ThrowsException()
{
    await Assert.ThrowsAsync<EntityNotFoundException<int>>(
        () => _service.GetByIdAsync(999));
}

[Fact]
public async Task Method_InvalidInput_ThrowsEntityNotFoundException()
{
    // Arrange
    var studentId = 999;
    _mockStudentRepo.Setup(r => r.GetStudentByIdAsync(studentId))
        .ThrowsAsync(new EntityNotFoundException<int>("Student", studentId));
    
    // Act & Assert
    var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(
        () => _service.GetStudentByIdAsync(studentId));
    
    Assert.Contains("Student", exception.Message);
    Assert.Contains(studentId.ToString(), exception.Message);
}
```

## Continuous Integration

Note: The project does not currently have CI/CD workflows configured. The following is an example configuration that could be added:

### GitHub Actions Example
```yaml
name: Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet test --verbosity normal
```
