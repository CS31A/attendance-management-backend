# Installation Guide

This guide provides step-by-step instructions for setting up the Attendance Management System in different environments.

## Prerequisites

Before installing the system, ensure you have the following prerequisites installed:

### Required Software

#### .NET 9.0 SDK
```bash
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0
# Verify installation
dotnet --version
# Should output: 9.0.x
```

#### SQL Server
Choose one of the following options:

**Option 1: SQL Server Express (Recommended for development)**
```bash
# Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
# Select "Express" edition for free local development
```

**Option 2: SQL Server Developer Edition**
```bash
# Download from: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
# Select "Developer" edition for full features
```

**Option 3: Docker SQL Server**
```bash
# Run SQL Server in Docker container
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
   -p 1433:1433 --name sqlserver --hostname sqlserver \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

#### Development Tools (Optional but Recommended)

**Visual Studio 2022**
- Download from: https://visualstudio.microsoft.com/
- Workloads: ASP.NET and web development, .NET desktop development

**Visual Studio Code**
- Download from: https://code.visualstudio.com/
- Extensions: C# Dev Kit, .NET Extension Pack

**SQL Server Management Studio (SSMS)**
- Download from: https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms

### System Requirements

#### Minimum Requirements
- **OS**: Windows 10/11, macOS 10.15+, or Linux (Ubuntu 18.04+)
- **RAM**: 4 GB minimum, 8 GB recommended
- **Storage**: 2 GB free space
- **Network**: Internet connection for package downloads

#### Recommended Requirements
- **OS**: Windows 11, macOS 12+, or Linux (Ubuntu 20.04+)
- **RAM**: 16 GB or more
- **Storage**: 10 GB free space (for development tools and databases)
- **CPU**: Multi-core processor

## Installation Steps

### 1. Clone the Repository

```bash
# Clone the repository
git clone <repository-url>
cd attendance-management-backend

# Verify project structure
ls -la
# Should show: attendance_monitoring/, attendance.testproject/, README.md, etc.
```

### 2. Environment Configuration

#### Create Environment File
```bash
# Navigate to the main project directory
cd attendance_monitoring

# Create .env file from template (if available)
cp .env.example .env

# Or create new .env file
touch .env
```

#### Configure Environment Variables
Edit the `.env` file with your specific configuration:

```env
# Database Configuration
ConnectionStrings__DefaultConnection=Server=localhost;Database=AttendanceDB;Trusted_Connection=true;TrustServerCertificate=true;

# For SQL Server Authentication (alternative to Trusted_Connection)
# ConnectionStrings__DefaultConnection=Server=localhost;Database=AttendanceDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;

# JWT Configuration
AppSettings__Token=your-super-secret-jwt-key-here-make-it-at-least-32-characters-long-for-security
AppSettings__Issuer=AttendanceMonitoringAPI
AppSettings__Audience=AttendanceMonitoringUsers

# Cookie Settings (Optional - uses defaults if not set)
CookieSettings__AccessTokenExpirationMinutes=15
CookieSettings__RefreshTokenExpirationDays=7

# Logging Configuration (Optional)
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning

# CORS Configuration (Optional)
AllowedOrigins__0=http://localhost:3000
AllowedOrigins__1=http://localhost:5173
```

#### Security Considerations for JWT Token
```bash
# Generate a secure JWT key (32+ characters)
# Option 1: Use online generator
# Visit: https://www.allkeysgenerator.com/Random/Security-Encryption-Key-Generator.aspx

# Option 2: Use PowerShell (Windows)
# [System.Web.Security.Membership]::GeneratePassword(64, 0)

# Option 3: Use OpenSSL (Linux/macOS)
openssl rand -base64 64

# Option 4: Use .NET CLI
dotnet user-secrets set "AppSettings:Token" "your-generated-key-here"
```

### 3. Database Setup

#### Option A: Using Entity Framework Migrations (Recommended)

```bash
# Install EF Core tools globally (if not already installed)
dotnet tool install --global dotnet-ef

# Verify EF tools installation
dotnet ef --version

# Navigate to project directory
cd attendance_monitoring

# Create database and apply migrations
dotnet ef database update

# Verify database creation
dotnet ef database list
```

#### Option B: Manual Database Creation

```sql
-- Connect to SQL Server using SSMS or Azure Data Studio
-- Create database manually
CREATE DATABASE AttendanceDB;

-- Then run migrations
dotnet ef database update 
```

or 

```bash
dotnet ef database update --project attendance_monitoring
```

#### Verify Database Setup
```bash
# Check migration history
dotnet ef migrations list

# Should show all applied migrations including:
# - InitialCreateWithIntId
# - UpdatedModels
# - RefreshTokens
# - AddSessionAndAttendanceEntities
# - RemoveRedundantEmailFields
```

### 4. Install Dependencies

```bash
# Navigate to main project directory
cd attendance_monitoring

# Restore NuGet packages
dotnet restore

# Verify packages are restored
ls bin/Debug/net9.0/
# Should show compiled assemblies
```

### 5. Build the Application

```bash
# Build the project
dotnet build

# Check for build errors
# Should output: Build succeeded. 0 Warning(s). 0 Error(s).

# Build in Release mode (optional)
dotnet build --configuration Release
```

### 6. Run the Application

#### Development Mode
```bash
# Run with hot reload (recommended for development)
dotnet watch run

# Or run normally
dotnet run

# The application will start on:
# HTTP: http://localhost:8080
# HTTPS: https://localhost:8081
```

#### Production Mode
```bash
# Build for production
dotnet build --configuration Release

# Run in production mode
dotnet run --configuration Release --environment Production
```

### 7. Verify Installation

#### Health Check
```bash
# Test the health endpoint
curl https://localhost:8081/api/health

# Expected response:
# {
#   "status": "Healthy",
#   "timestamp": "2024-01-01T00:00:00Z",
#   "database": "Connected",
#   "version": "1.0.0"
# }
```

#### API Documentation
Open your browser and navigate to:
- **Swagger UI**: `https://localhost:8081/swagger`
- **Scalar Documentation**: `https://localhost:8081/scalar/v1`

#### Test Registration
```bash
# Test user registration endpoint
curl -X POST https://localhost:8081/api/account/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Test123!",
    "confirmPassword": "Test123!",
    "role": "Student",
    "firstname": "Test",
    "lastname": "User",
    "sectionId": 1,
    "isRegular": true
  }'
```

## Testing Setup

### Run Unit Tests
```bash
# Navigate to test project
cd attendance.testproject

# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "ClassName=AccountControllerTest"
```

### Integration Tests
```bash
# Run integration tests (if available)
dotnet test --filter "Category=Integration"
```

## Development Environment Setup

### IDE Configuration

#### Visual Studio 2022
1. Open `attendance.sln` solution file
2. Set `attendance_monitoring` as startup project
3. Configure debugging settings in `Properties/launchSettings.json`
4. Install recommended extensions:
   - Entity Framework Core Power Tools
   - REST Client
   - Git Extensions

#### Visual Studio Code
1. Open project folder
2. Install recommended extensions:
   - C# Dev Kit
   - .NET Extension Pack
   - REST Client
   - GitLens
3. Configure `launch.json` and `tasks.json` for debugging

### Git Configuration
```bash
# Configure Git hooks (optional)
git config core.hooksPath .githooks

# Set up pre-commit hooks for code formatting
# Create .githooks/pre-commit file with formatting commands
```

## Troubleshooting

### Common Issues

#### Database Connection Issues
```bash
# Test SQL Server connection
sqlcmd -S localhost -E -Q "SELECT @@VERSION"

# Check connection string format
# Windows Authentication: Trusted_Connection=true
# SQL Authentication: User Id=sa;Password=YourPassword
```

#### Port Conflicts
```bash
# Check if ports are in use
netstat -an | grep :8080
netstat -an | grep :8081

# Change ports in appsettings.json if needed
```

#### SSL Certificate Issues
```bash
# Trust development certificate
dotnet dev-certs https --trust

# Clean and regenerate certificates
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

#### Package Restore Issues
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages with verbose output
dotnet restore --verbosity detailed
```

### Getting Help

#### Log Files
- Application logs: Check console output or configured log files
- IIS logs: `%SystemRoot%\System32\LogFiles\W3SVC1\`
- Event logs: Windows Event Viewer → Application logs

#### Debug Mode
```bash
# Run with detailed logging
dotnet run --environment Development

# Enable Entity Framework logging
# Add to appsettings.Development.json:
# "Logging": {
#   "LogLevel": {
#     "Microsoft.EntityFrameworkCore": "Information"
#   }
# }
```

#### Support Resources
- Check GitHub Issues for known problems
- Review API documentation at `/scalar/v1`
- Consult Entity Framework Core documentation
- ASP.NET Core troubleshooting guides

## Next Steps

After successful installation:

1. **Configure Authentication**: Set up user roles and permissions
2. **Create Test Data**: Add sample courses, sections, and users
3. **Test API Endpoints**: Use Swagger UI or Postman to test functionality
4. **Set Up Frontend**: Configure your frontend application to connect to the API
5. **Configure Production**: Prepare for production deployment

For detailed configuration options, see the [Configuration Reference](./configuration-reference.md).
For deployment instructions, see the [Deployment Guide](./deployment-guide.md).