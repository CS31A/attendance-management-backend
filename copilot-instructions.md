<!-- GSD:project-start source:PROJECT.md -->
## Project

**Attendance Management Backend**

Attendance Management Backend is an existing ASP.NET Core Web API for educational institutions to manage users, courses, schedules, sessions, enrollments, QR-based attendance, and reporting. It serves both a Vue web client and a Flutter mobile client with shared domain behavior and different auth flows. This milestone initializes GSD with a brownfield context and a first focus on stabilization rather than major feature expansion.

**Core Value:** The system must deliver reliable, regression-resistant attendance and identity workflows for API consumers while remaining maintainable for backend engineers.

### Constraints

- **Tech stack**: ASP.NET Core 10 + EF Core + SQL Server — retain compatibility with established backend patterns
- **Client compatibility**: Preserve existing API behavior expected by Vue and Flutter consumers — avoid breaking contracts
- **Scope**: Prioritize stabilization and quality hardening before net-new capabilities — chosen to reduce regression risk
- **Workflow**: GSD initialization artifacts should be generated and committed incrementally — preserves recoverability
<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->
## Technology Stack

## Languages
- C# 12 - All backend logic, API controllers, services, and database models
- T-SQL - SQL Server stored procedures and database schemas (inferred from SqlServer provider)
- PowerShell/Bash - Docker build scripts and deployment automation
## Runtime
- .NET 10.0 (Target Framework: net10.0)
- NuGet - Manages all .NET packages
- Lockfile: Present via `.csproj` and implicit lock mechanism in NuGet
## Frameworks
- ASP.NET Core 10.0.2 - Web API framework with Kestrel server
- Entity Framework Core 10.0.2 - ORM for database access and relationships
- Microsoft.AspNetCore.Identity 10.0.2 - User authentication and role management
- SignalR 10.0.2 - WebSocket-based real-time notifications (see `Extensions/SignalRServiceExtensions.cs`)
- Scalar.AspNetCore 2.12.10 - Interactive API documentation UI
- Swashbuckle.AspNetCore 10.1.0 - Swagger/OpenAPI documentation generation
- xunit 2.9.3 - Unit testing framework (`attendance.testproject/attendance.testproject.csproj`)
- Moq 4.20.72 - Mocking library for unit tests
- Microsoft.AspNetCore.TestHost 10.0.2 - In-memory hosting for integration tests
- Microsoft.AspNetCore.Mvc.Testing 10.0.2 - WebApplicationFactory for testing ASP.NET Core apps
- NetArchTest.Rules 1.3.2 - Architecture testing (enforces layering conventions)
- coverlet.collector 6.0.4 - Code coverage measurement
- Microsoft.EntityFrameworkCore.Design 10.0.2 - EF Core tooling (migrations, scaffolding)
- Microsoft.VisualStudio.Web.CodeGeneration.Design 10.0.2 - Scaffolding templates
## Key Dependencies
- dapper 2.1.66 - Lightweight ORM for raw SQL queries (used alongside EF Core)
- QRCoder 1.7.0 - QR code generation for attendance sessions (`Classes/QrCode.cs`)
- System.IdentityModel.Tokens.Jwt 8.15.0 - JWT token validation and signing
- Microsoft.AspNetCore.Authentication.JwtBearer 10.0.2 - JWT authentication handler
- Microsoft.EntityFrameworkCore.SqlServer 10.0.2 - SQL Server provider for EF Core
- Microsoft.EntityFrameworkCore.InMemory 10.0.2 - In-memory database for testing
- Microsoft.AspNetCore.OpenApi 10.0.2 - OpenAPI specification generation
- DotNetEnv 3.1.1 - Load environment variables from `.env` files
- Microsoft.AspNetCore.DataProtection - Built-in for cookie/token encryption
## Configuration
- `.env` file support via DotNetEnv (loaded in `Program.cs` line 6)
- `appsettings.json` - Default configuration
- `appsettings.Development.json` - Development overrides
- Environment variables override configuration hierarchy
- `.csproj` files: Define targets, dependencies, and build properties
- `Dockerfile` - Multi-stage build for containerization (base: mcr.microsoft.com/dotnet/aspnet:10.0)
## Platform Requirements
- .NET 10.0 SDK
- Visual Studio 2022 (recommended) or VS Code with C# extension
- SQL Server LocalDB or SQL Server instance (local or remote)
- Node.js (if managing frontend alongside backend)
- .NET 10.0 Runtime
- SQL Server 2019+ (or compatible)
- Docker support (see `Dockerfile`)
- Kestrel server running on port 8080 (HTTP) / 8081 (HTTPS)
- Reverse proxy (nginx/IIS recommended) for production
## Database
- SQL Server - Connection string in `appsettings.json`
- Entity Framework Core Code-First Migrations
- Migration files in `Migrations/` directory
- Database context: `Data/ApplicationDbContext.cs`
## Containers
- Base image: `mcr.microsoft.com/dotnet/aspnet:10.0`
- Build image: `mcr.microsoft.com/dotnet/sdk:10.0`
- Entrypoint: `dotnet attendance_monitoring.dll`
- Exposed ports: 8080 (HTTP), 8081 (HTTPS)
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

## Naming Patterns
- PascalCase for class files: `StudentController.cs`, `StudentService.cs`, `IStudentRepository.cs`
- DTO files match pattern: `StudentListDto.cs`, `CreateStudent.cs`, `UpdateStudent.cs`
- Test files follow pattern: `StudentControllerTest.cs`, `SessionServiceTest.cs`
- No pluralization on most service files
- PascalCase for all public methods: `GetStudentsAsync()`, `CreateStudentAsync()`, `SearchStudentsByNameAsync()`
- Async methods end with `Async` suffix: `GetStudentByIdAsync()`, `UpdateStudentAsync()`, `SoftDeleteStudentAsync()`
- Private methods also use PascalCase with underscore prefix for fields: `_studentRepository`, `_logger`
- Test methods use pattern `MethodName_Scenario_ExpectedResult`: `GetStudents_ReturnsOkResult_WithStudentsList()`, `SearchStudentsByName_WithValidQuery_ReturnsOkResult()`
- camelCase for local variables: `expectedStudents`, `searchTerm`, `pageNumber`
- PascalCase for properties and constants
- Private/protected fields use camelCase with leading underscore: `_mockStudentService`, `_studentController`, `_testUser`, `_httpContext`
- PascalCase for all class names: `Student`, `StudentService`, `StudentController`
- Interface names start with `I`: `IStudentService`, `IStudentRepository`, `IAccountService`
- Exception classes end with `Exception`: `EntityServiceException`, `EntityNotFoundException<T>`, `EntityUnauthorizedException`
- DTO suffix pattern: `StudentListDto`, `CreateStudent`, `UpdateStudent`, `StudentSubjectResponseDto`
- Controller suffix: `StudentController`, `AccountController`, `SessionController`
- Service suffix: `StudentService`, `SessionService`, `FingerprintService`
- Repository suffix: `StudentRepository`, `AccountRepository`, `SessionRepository`
## Code Style
- Target Framework: `.NET 10.0` (`net10.0` in `attendance_monitoring.csproj` and `attendance.testproject.csproj`)
- Nullable reference types enabled: `<Nullable>enable</Nullable>`
- Implicit usings enabled: `<ImplicitUsings>enable</ImplicitUsings>`
- Documentation XML generation enabled: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- Namespace format: `namespace attendance_monitoring.Controllers;` (file-scoped namespaces with semicolon)
- Uses constructor parameter injection pattern: `public StudentController(IStudentService studentService, ILogger<StudentController> logger) : ControllerBase`
- No explicit .editorconfig or StyleCop configuration detected
- Code appears to follow standard .NET conventions implicitly
- XML documentation warnings suppressed: `<NoWarn>$(NoWarn);1591</NoWarn>` (missing XML comments don't warn)
## Import Organization
- No aliases detected; fully qualified names used throughout
- Implicit usings reduce need for repetitive imports
## Error Handling
- Typed exception hierarchy with custom exceptions in `attendance_monitoring/Exceptions/`:
- Try-catch blocks in service layer with exception re-throwing:
- Controllers catch exceptions and map to HTTP status codes:
- Global exception handler middleware in `Extensions/WebApplicationExtensions/GlobalExceptionHandlerExtension.cs` for centralized error responses
## Logging
- Constructor injection of `ILogger<ControllerName>` or `ILogger<ServiceName>`
- Structured logging with placeholders: `_logger.LogInformation("Getting student with ID: {Id}", id);`
- Log levels used:
## Comments
- XML documentation comments (`///`) on all public types, methods, and properties
- Explanatory comments for complex logic or non-obvious behavior
- Region comments (`#region`/`#endregion`) to organize method groups in services and repositories
- Inline comments explain "why" not "what" (code shows what it does)
- Uses C# XML documentation format:
- Applied to controllers (for OpenAPI/Swagger documentation) and service interfaces
- Performance notes documented: "Performance: Single query, no navigation properties loaded."
## Function Design
- Service methods typically 10-50 lines (exception handling adds length)
- Controllers kept lean with service delegation
- Architecture validation test enforces 500-line budget for new services: `ServiceArchitectureGuardrailTests.cs`
- Constructor injection for dependencies (services, repositories, loggers)
- Method parameters follow standard order: IDs, DTOs, ClaimsPrincipal for auth
- Query parameters in controllers use `[FromQuery]` attributes: `SearchStudentsByName(string query, int pageNumber = 1, int pageSize = 50)`
- Request bodies use `[FromBody]` implicitly for DTOs: `PatchStudent(int id, UpdateStudent updateStudent)`
- Controllers return `Task<ActionResult<T>>` for async endpoints
- Services return `Task<T>` or `Task<IList<T>>` or `Task`
- Repositories return `Task<T?>` for single items, `Task<IList<T>>` for collections
- String returns used for error messages: `Task<string?>` in soft/hard delete operations
## Module Design
- Controllers: `[ApiController]` attribute with `[Route]` for endpoint routing
- Services: Public interfaces (`IStudentService`) with implementation in concrete service class
- Repositories: Public interfaces (`IStudentRepository`) with repository pattern implementation
- Constants: Centralized in `Constants/` directory (e.g., `RoleConstants.Instructor`)
- No explicit barrel files (index.ts style) detected
- Namespaces organize exports: `namespace attendance_monitoring.Services;`
- Interface-based dependency injection enables loose coupling
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

## Pattern Overview
- Clean separation between Controllers, Services, Repositories, and Data layers
- Dependency Injection throughout entire stack via `IServiceCollection` extensions
- Interface-driven design with concrete implementations
- Domain-driven entities with relational mappings
- Async/await throughout for non-blocking I/O
- Composite service patterns for complex operations (e.g., QrCodeService aggregates 4 sub-services)
## Layers
- Purpose: Handle HTTP requests, validation, and responses
- Location: `attendance_monitoring/Controllers/`
- Contains: 16 controller classes (AccountController, StudentController, QrCodeController, etc.)
- Depends on: IServices (interfaces only), UserContextService for authorization
- Used by: Client applications via REST API
- Pattern: Dependency injection via constructor, async action methods returning ActionResult<T>
- Purpose: Implement business rules, validate data, orchestrate repository calls
- Location: `attendance_monitoring/Services/`
- Contains: 26 service implementations and 19 service interfaces in `IServices/`
- Depends on: IRepository interfaces, other IServices, UserContextService, ILogger
- Used by: Controllers, other Services
- Pattern: Constructor injection of repositories, async Task-based operations, exception handling
- Location: `attendance_monitoring/Services/QrCode/`
- Contains: QrCodeService (orchestrator), QrCodeGenerationService, QrCodeQueryService, QrCodeWriteService, QrCodeScanService
- Purpose: Compartmentalize complex QrCode logic into focused, testable units
- Example: QrCodeGenerationService handles hash generation with retry logic and conflict resolution
- Purpose: Abstract database operations, provide type-safe data access
- Location: `attendance_monitoring/Repositories/` and `attendance_monitoring/IRepository/`
- Contains: 14 repository implementations, 16 repository interfaces
- Depends on: ApplicationDbContext, Entity models
- Used by: Services
- Pattern: Implement IRepository interface, use EF Core DbSet operations, AsNoTracking for reads
- Purpose: Entity definitions and database context configuration
- Location: `attendance_monitoring/Data/ApplicationDbContext.cs`, `attendance_monitoring/Classes/`
- Contains: DbContext with 14 DbSet properties, 19 entity classes
- Depends on: Microsoft.EntityFrameworkCore, Entity definitions
- Features: Unique constraints, indexes, shadow properties, migrations
## Data Flow
- **Transactional State**: Handled at database level via EF Core transactions
- **User Context State**: `UserContextService` maintains current user context from ClaimsPrincipal
- **Real-time State**: SignalR via `NotificationHub` for attendance updates
- **Session State**: Stateless REST API; client manages session tokens
- **Notification Preferences**: `InMemoryPreferenceService` (singleton) caches user preferences
## Key Abstractions
- Purpose: Abstract data access, provide testable seams, allow switching data sources
- Examples: `IStudentRepository`, `IAttendanceRepository`, `IQrCodeRepository`
- Pattern: Generic/specific methods like `GetAllAsync()`, `GetByIdAsync()`, entity-specific queries
- Benefits: Decouples services from EF Core, enables in-memory testing
- Purpose: Define contracts for business operations, enable mocking, support composition
- Examples: `IAccountService`, `IQrCodeService`, `IAttendanceService`
- Pattern: Async Task-based methods returning DTOs, exception-driven error handling
- Composition: Complex services (QrCodeService) depend on multiple sub-services
- Purpose: Map domain models to API contracts, reduce over-fetching, version responses
- Locations: `Models/DTO/` with Request and Response subdirectories
- Examples: `CreateAttendanceRequest` → `AttendanceRecordResponseDto`, `RegisterDto` → `RegisterResponseDto`
- Benefit: Controllers and Services never expose raw entities; API schema decoupled from schema
- Purpose: Complex object creation with validation
- Example: `UserFactory.CreateUserAsync()` handles role assignment and user type determination
- Location: `Classes/Factory/UserFactory.cs`
- Purpose: Run long-running or scheduled operations asynchronously
- Examples: `BlacklistedTokenCleanupService`, `RoleInitializationBackgroundService`, `OrphanedUserCleanupService`
- Implementation: Inherit from `BackgroundService`, override `ExecuteAsync()`
- Registration: Via `AddHostedService<T>()` in dependency injection
## Entry Points
- Location: `Program.cs`
- Triggers: Application start
- Responsibilities:
- Location: `Controllers/`
- Triggers: HTTP requests matching routes
- Examples: `POST /api/account/register`, `GET /api/students`, `POST /api/attendance`
- Responsibilities: Parse requests, delegate to services, return HTTP responses
- Location: `Hubs/NotificationHub.cs`
- Triggers: WebSocket connections from clients
- Responsibilities: Manage real-time notifications for attendance updates, user connection tracking
## Error Handling
- **Custom Exceptions**: `EntityNotFoundException<T>`, `EntityAlreadyExistsException<T>`, `EntityUnauthorizedException`, `EntityServiceException`, `ValidationException`
- **Location**: `Exceptions/` directory
- **Global Handler**: `ExceptionHandlingExtensions.UseGlobalExceptionHandler()`
- **Mapping**:
- **Response Format**: Standardized `ErrorResponseDto` with message and optional details
- **Logging**: All exceptions logged via ILogger at appropriate level (Warning/Error)
## Cross-Cutting Concerns
- Provider: JWT Bearer tokens
- Implementation: `Microsoft.AspNetCore.Authentication.JwtBearer`
- Claims: Extracted into `ClaimsPrincipal` passed to service methods
- Authorization: Policy-based via `AddAuthorizationPolicies()` in `AuthenticationServiceExtensions.cs`
- Token Refresh: `RefreshTokenService` with refresh token rotation
- Token Blacklist: `BlacklistedTokenService` with background cleanup
- Framework: `ILogger<T>` dependency injection
- Configuration: `AddApplicationLogging()` in `LoggingServiceExtensions.cs`
- Levels: Information, Warning, Error used appropriately
- Patterns: User context, operation tracking, performance metrics
- Example: `logger.LogInformation("Creating attendance record for StudentId: {StudentId}", request.StudentId)`
- Data Annotations: Used on request DTOs (Required, StringLength, Range, etc.)
- ModelState: Checked in controllers before service calls
- Custom Validation: Via `IValidatableObject` implementation (e.g., `RegisterDto`)
- Business Logic Validation: In services (e.g., enrollment verification, status checks)
- **Compression**: Selective response compression for GET endpoints via middleware
- **Tracking**: AsNoTracking() on read-only queries to reduce memory
- **Projection**: DTOs used for database projection (SELECT specific columns)
- **Indexing**: Database indexes on frequently queried columns (UserId, SessionId, QrHash, TokenHash)
- **Monitoring**: Response time headers (`X-Response-Time`) and logging in middleware
- **Unique Constraints**: Database-level (email uniqueness, token hash uniqueness)
- **Foreign Keys**: EF Core enforces referential integrity
- **Composite Uniqueness**: Attendance record composite unique index (StudentId, SessionId) prevents duplicate attendance
- **Transactions**: EF Core handles implicit transactions for SaveChangesAsync()
- Configuration: `AddCorsPolicy()` in `ResponseHandlingExtensions.cs`
- Purpose: Enable cross-origin requests from web clients
- Service: `AddDataProtection()` registered via ASP.NET Core
- Use: JWT encryption/decryption, sensitive data protection
<!-- GSD:architecture-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
