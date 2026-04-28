# Development Guide

This guide provides best practices and workflows for developing the Attendance Management System.

## Development Environment Setup

### Prerequisites
- .NET 10.0 SDK
- SQL Server (LocalDB, Express, or Full)
- Visual Studio 2022 or VS Code with C# Dev Kit
- Git

### Initial Setup

```bash
# Clone the repository
git clone <repository-url>
cd attendance-management-backend/attendance_monitoring

# Copy environment file
cp .env.example .env

# Edit .env with your configuration
# - Set ConnectionStrings__DefaultConnection
# - Set AppSettings__Token (min 32 characters)
# - Set AppSettings__Issuer
# - Set AppSettings__Audience

# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update

# Run the application
dotnet run
```

## Project Structure

```
attendance_monitoring/
├── Controllers/          # API endpoints
├── Services/            # Business logic
├── IServices/           # Service interfaces
├── Repositories/        # Data access layer
├── IRepository/         # Repository interfaces
├── Classes/             # Entity models
├── Models/DTO/          # Request/Response DTOs
├── Exceptions/          # Custom exceptions
├── Extensions/          # Service extensions
├── Migrations/          # EF Core migrations
├── Data/                # DbContext
└── Constants/           # Application constants
```

## Code Conventions

### Naming Conventions
- **Controllers**: `{Entity}Controller.cs` (e.g., `StudentController.cs`)
- **Services**: `{Entity}Service.cs` with `I{Entity}Service.cs` interface
- **Repositories**: `{Entity}Repository.cs` with `I{Entity}Repository.cs` interface
- **DTOs**: 
  - Requests: `Create{Entity}Dto`, `Update{Entity}Dto`
  - Responses: `{Entity}ResponseDto`, `{Entity}ListResponseDto`

### Async Patterns
All I/O operations should be async:
```csharp
public async Task<Student?> GetStudentByIdAsync(int id)
{
    return await _context.Students
        .FirstOrDefaultAsync(s => s.Id == id);
}
```

### Exception Handling
Use custom exceptions for business logic errors:
```csharp
// Throw specific exceptions
throw new NotFoundException($"Student with ID {id} not found");
throw new ConflictException("Student already exists");
throw new UnauthorizedException("Invalid credentials");
```

## Adding New Features

### 1. Create Entity
```csharp
// Classes/NewEntity.cs
public class NewEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### 2. Add to DbContext
```csharp
// Data/ApplicationDbContext.cs
public DbSet<NewEntity> NewEntities { get; set; }
```

### 3. Create Repository
```csharp
// IRepository/INewEntityRepository.cs
public interface INewEntityRepository : IBaseRepository<NewEntity>
{
    Task<NewEntity?> GetByNameAsync(string name);
}

// Repositories/NewEntityRepository.cs
public class NewEntityRepository : BaseRepository<NewEntity>, INewEntityRepository
{
    // Implementation
}
```

### 4. Create Service
```csharp
// IServices/INewEntityService.cs
public interface INewEntityService
{
    Task<NewEntity> CreateAsync(CreateNewEntityDto dto);
    Task<NewEntity?> GetByIdAsync(int id);
}

// Services/NewEntityService.cs
public class NewEntityService : INewEntityService
{
    // Implementation
}
```

### 5. Create Controller
```csharp
// Controllers/NewEntityController.cs
[ApiController]
[Route("api/[controller]")]
public class NewEntityController : ControllerBase
{
    // Implementation
}
```

### 6. Register Services
```csharp
// Extensions/ServiceCollectionExtensions.cs
services.AddScoped<INewEntityRepository, NewEntityRepository>();
services.AddScoped<INewEntityService, NewEntityService>();
```

### 7. Create Migration
```bash
dotnet ef migrations add AddNewEntity
dotnet ef database update
```

## Running the Application

### Development Mode
```bash
cd attendance_monitoring
dotnet run

# Or with hot reload
dotnet watch run
```

### Available URLs
- **API**: https://localhost:8081
- **Swagger UI**: https://localhost:8081/swagger
- **Scalar Docs**: https://localhost:8081/scalar/v1

## Common Tasks

### Creating Migrations
```bash
# Add migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific project
dotnet test attendance.testproject/
```

## Debugging Tips

### Enable Detailed Logging
In `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Database Query Logging
EF Core queries are logged at Information level in development.

### Common Issues

1. **JWT Token errors**: Ensure token is at least 32 characters
2. **Database connection**: Verify connection string and SQL Server is running
3. **CORS errors**: Add frontend URL to `CorsSettings__AllowedOrigins`

## API Testing

### Using Swagger
Navigate to `/swagger` to test endpoints interactively.

### Using HTTP Files
The project includes `attendance_monitoring.http` for VS Code REST Client extension.

### Authentication Flow
1. Register: `POST /api/account/register`
2. Login: `POST /api/account/login`
3. Use returned `accessToken` in Authorization header: `Bearer {token}`
