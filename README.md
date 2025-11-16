# Attendance Monitoring System

A comprehensive attendance tracking system built with ASP.NET Core 9.0, Entity Framework Core, and Cookie-based Authentication for Web and JWT authentication for mobile. This system enables educational institutions to manage students, instructors, courses, and track attendance efficiently.

## 🚀 Features

- **User Management**: Student, Instructor, and Admin role-based authentication
- **Course Management**: Create and manage courses, sections, and subjects
- **Schedule Management**: Assign classrooms and time slots to sections
- **Session Management**: Create, start, end, and cancel class sessions
- **Student Enrollment**: Enroll students in sections and track enrollment status
- **Attendance Recording**: Record and track student attendance for sessions
- **Attendance Reports**: View attendance history, summaries, and statistics
- **QR Code Integration**: Generate QR codes for attendance tracking
- **Soft Delete**: Safe deletion and restoration of students and instructors
- **Token Security**: JWT authentication with refresh tokens and blacklist system
- **Background Services**: Automatic cleanup of expired tokens
- **API Documentation**: Interactive Swagger/Scalar documentation

## 🏗️ Technology Stack

- **Framework**: ASP.NET Core 9.0 Web API
- **Database**: Entity Framework Core with SQL Server
- **Authentication**: JWT Bearer tokens + ASP.NET Core Identity
- **Testing**: xUnit with Moq
- **Documentation**: Swagger/OpenAPI with Scalar UI
- **Additional**: QRCoder, DotNetEnv

## 📋 Prerequisites

Before setting up the project, ensure you have the following installed:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)

## 🛠️ Backend Setup for Development

### 1. Clone the Repository

```bash
git clone <repository-url>
cd attendance-monitoring-system
```

### 2. Environment Configuration

Create a `.env` file in the `attendance_monitoring` directory:

```bash
cd attendance_monitoring
cp .env.example .env
```

Edit the `.env` file with your configuration:

```env
# Database Configuration
ConnectionStrings__DefaultConnection=Server=localhost;Database=AttendanceDB;Trusted_Connection=true;TrustServerCertificate=true;

# JWT Configuration
AppSettings__Token=your-super-secret-jwt-key-here-make-it-at-least-32-characters-long
AppSettings__Issuer=AttendanceMonitoringAPI
AppSettings__Audience=AttendanceMonitoringUsers

# Cookie Settings (Optional - uses defaults if not set)
CookieSettings__AccessTokenExpirationMinutes=15
CookieSettings__RefreshTokenExpirationDays=7
```

### 3. Database Setup

Navigate to the main project directory:

```bash
cd attendance_monitoring
```

#### Option A: Using Entity Framework Migrations (Recommended)

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Update database with existing migrations
dotnet ef database update

# Verify the database was created
dotnet ef database list
```

### 4. Install Dependencies

```bash
# Restore NuGet packages
dotnet restore
```

### 5. Build the Application

```bash
# Build the project to check for errors and if it compiles successfully
dotnet build
```

### 6. Run the Application

```bash
# Run in development mode
dotnet run

# Or run with watch mode (auto-reload on changes)
dotnet watch run
```

The application will start and be available at:
- HTTP: `http://localhost:8080`
- HTTPS: `https://localhost:8081`
- Swagger UI: `http://localhost:8080/swagger`
- Scalar API Docs: `http://localhost:8080/scalar/v1`

### 8. Verify Installation

1. Open your browser and navigate to `https://localhost:8081/scalar`
2. You should see the Scalar API documentation
3. Test the health check endpoint: `GET /api/HealthCheck`

## 🧪 Running Tests

### Unit Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test attendance.testproject/
```

## 📊 Database Management

### Creating New Migrations

```bash
# Add a new migration
dotnet ef migrations add YourMigrationName

# Update database
dotnet ef database update
```

### Rolling Back Migrations

```bash
# Rollback to a specific migration
dotnet ef database update PreviousMigrationName

# Remove the last migration (if not applied to database)
dotnet ef migrations remove
```

### Database Reset (Development)

```bash
# Drop database
dotnet ef database drop

# Recreate database with all migrations
dotnet ef database update
```

## 🔐 Default Roles and Authentication

The system automatically creates the following roles on startup:
- **Admin**: Full system access
- **Instructor**: Manage sections and attendance
- **Student**: View own attendance records

### API Authentication

The API uses JWT Bearer tokens. To authenticate for mobile:

1. Register a new user via `/api/account/register`
2. Login via `/api/account/login` to get access and refresh tokens
3. Include the access token in the `Authorization` header: `Bearer <your-token>`

For web:

1. Register a new user via `/api/account/register`
2. Login via `/api/account/web/login`, the tokens will automatically be stored in your cookies
3. The tokens will automatically be included in your requests

## 📁 Project Structure

```
attendance-monitoring-system/
├── attendance_monitoring/           # Main API project
│   ├── Controllers/                # API controllers
│   ├── Services/                   # Business logic layer
│   ├── Repositories/               # Data access layer
│   ├── Classes/                    # Entity models
│   ├── Data/                       # DbContext
│   ├── Migrations/                 # EF Core migrations
│   ├── Models/DTO/                 # Data transfer objects
│   └── IServices/, IRepository/    # Interfaces
├── attendance.testproject/         # Unit tests
├── attendance_monitoring.tests/    # Additional tests
└── README.md                       # This file
```

## 🔧 Configuration Options

### CORS Configuration

The application is configured to allow requests from `http://localhost:5173` (typical Vite/React dev server). Update the CORS policy in `Program.cs` if your frontend runs on a different port.

### JWT Configuration

- **Access Token Expiration**: 15 minutes (configurable)
- **Refresh Token Expiration**: 7 days (configurable)
- **Token Cleanup**: Automatic background service removes expired tokens

### Logging

The application uses structured logging with different levels:
- **Information**: General application flow
- **Warning**: Unusual but expected events
- **Error**: Error events that allow the application to continue

## 🚀 Deployment

### Production Considerations

1. **Environment Variables**: Set production values for JWT tokens and connection strings
2. **Database**: Use a production SQL Server instance
3. **HTTPS**: Ensure HTTPS is properly configured
4. **Logging**: Configure appropriate logging providers
5. **CORS**: Update CORS policy for production domains

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 API Documentation

Once the application is running, you can access:

- **Swagger UI**: `http://localhost:8080/swagger` - Interactive API documentation
- **Scalar Documentation**: `http://localhost:8080/scalar/v1` - Modern API documentation
- **OpenAPI Spec**: `http://localhost:8080/swagger/v1/swagger.json` - Raw OpenAPI specification

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Verify SQL Server is running
   - Check connection string format
   - Ensure database exists or migrations are applied

2. **JWT Token Issues**
   - Ensure JWT secret key is at least 32 characters
   - Check token expiration settings
   - Verify Issuer and Audience configuration

3. **Port Conflicts**
   - Default ports are 8080 (HTTP) and 8081 (HTTPS)
   - Change ports in `appsettings.json` if needed

4. **CORS Issues**
   - Update CORS policy in `Program.cs` for your frontend URL
   - Ensure credentials are included if using cookies

### Getting Help

- Check the application logs for detailed error messages
- Review the Swagger documentation for API usage
- Ensure all prerequisites are properly installed

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
