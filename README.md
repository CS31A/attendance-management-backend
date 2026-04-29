# Attendance Monitoring System

A comprehensive attendance tracking system built with ASP.NET Core 10.0, Entity Framework Core, and Cookie-based Authentication for Web and JWT authentication for mobile. This system enables educational institutions to manage students, instructors, courses, and track attendance efficiently.

## 🚀 Features

- **User Management**: Student, Instructor, and Admin role-based authentication with unified user lifecycle management
- **Course Management**: Create and manage courses, sections, and subjects
- **Schedule Management**: Assign classrooms and time slots to sections
- **Session Management**: Create, start, end, and cancel class sessions
- **Student Enrollment**: Enroll students in sections and track enrollment status
- **Attendance Recording**: Record and track student attendance for sessions
- **Attendance Reports**: View attendance history, summaries, and statistics
- **QR Code Integration**: Generate QR codes for attendance tracking
- **Soft Delete**: Safe deletion and restoration of students, instructors, and admins
- **User Lifecycle Management**: Complete user management with soft/hard delete and restore capabilities
- **Token Security**: JWT authentication with refresh tokens and blacklist system
- **Background Services**: Automatic cleanup of expired tokens
- **API Documentation**: Interactive Swagger/Scalar documentation

## 🏗️ Technology Stack

- **Framework**: ASP.NET Core 10.0 Web API
- **Database**: Entity Framework Core with SQL Server
- **Authentication**: JWT Bearer tokens + ASP.NET Core Identity
- **Testing**: xUnit with Moq
- **Documentation**: Swagger/OpenAPI with Scalar UI
- **Additional**: QRCoder, DotNetEnv

## 🖥️ System Consumers

This backend API serves two client applications:

- **Vue Frontend** - Web application using cookie-based authentication
  - Authentication: `/api/account/web/login`
  - Tokens stored in HTTP-only cookies
  - CORS configured for `http://localhost:5173` (default Vite dev server)

- **Flutter Mobile App** - Mobile application using JWT Bearer tokens
  - Authentication: `/api/account/login`
  - Tokens managed by client application
  - Access token in `Authorization: Bearer <token>` header

## 📚 Documentation Hub

For more detailed documentation, see the `/docs` folder:

### Core Documentation
- **[Project Overview](./docs/project-overview.md)** - System architecture, features, and technology stack
- **[API Reference](./docs/api-reference.md)** - Complete API endpoints documentation
- **[Database Schema](./docs/database-schema.md)** - Entity relationships and data model
- **[Architecture Guide](./docs/architecture-guide.md)** - System design patterns and layers

### Setup & Configuration
- **[Installation Guide](./docs/installation-guide.md)** - Step-by-step setup instructions
- **[Configuration Reference](./docs/configuration-reference.md)** - Environment variables and settings
- **[Deployment Guide](./docs/deployment-guide.md)** - Production deployment strategies

### Development
- **[Development Guide](./docs/development-guide.md)** - Development workflow and best practices
- **[Testing Guide](./docs/testing-guide.md)** - Unit testing and integration testing

### Advanced Topics
- **[Authentication & Authorization](./docs/auth-guide.md)** - JWT, cookies, and role-based access
- **[QR Code System](./docs/qr-code-guide.md)** - QR code generation and attendance tracking

## 📋 Prerequisites

Before setting up the project, ensure you have the following installed:

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)

## 🚀 Quick Start

For detailed installation instructions, see the **[Installation Guide](./docs/installation-guide.md)**.

### Quick Setup Overview

```bash
# 1. Clone and setup
git clone <repository-url>
cd attendance-management-backend
cd attendance_monitoring
cp .env.example .env

# 2. Configure your .env file (see Installation Guide for details)

# 3. Install tools and setup database
dotnet tool install --global dotnet-ef
dotnet restore
dotnet ef database update

# 4. Run the application
dotnet run
```

**Application URLs:**
- API: `https://localhost:8081`
- Swagger UI: `https://localhost:8081/swagger`
- Scalar Docs: `https://localhost:8081/scalar/v1`
- Health Live: `http://localhost:8080/health/live`
- Health Ready: `http://localhost:8080/health/ready`
- Health Detailed: `http://localhost:8080/health`

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
├── docs/                           # Detailed documentation
└── README.md                       # This file
```

## 📊 Entity Relationships

```
IdentityUser (1) ←→ (1) Student/Instructor/Admin
Course (1) ←→ (M) Section
Section (1) ←→ (M) Student (primary enrollment)
Student (M) ←→ (M) Subject (via StudentEnrollment for irregular students)
Instructor (1) ←→ (M) Schedule
Schedule (M) ←→ (1) Subject, Section, Classroom
Session (M) ←→ (1) Schedule
AttendanceRecord (M) ←→ (1) Session, Student
```

## 🔧 Configuration Options

### CORS Configuration

The application's CORS policy is now configurable via environment variables. Add the following to your `.env` file:

```env
# CORS Settings - Separate multiple origins with semicolons (;)
CorsSettings__AllowedOrigins=http://localhost:5173;http://localhost:3000
```

If not configured, the application defaults to `http://localhost:5173` (typical Vite/React dev server).

### JWT Configuration

- **Access Token Expiration**: 15 minutes (configurable)
- **Refresh Token Expiration**: 7 days (configurable)
- **Token Cleanup**: Automatic background service removes expired tokens

### Logging

The application uses structured logging with different levels:
- **Information**: General application flow
- **Warning**: Unusual but expected events
- **Error**: Error events that allow the application to continue

## 🏥 Health Check Endpoints

The API exposes three standard health check endpoints for monitoring and container orchestration:

### Liveness Probe
```http
GET /health/live
```
- Returns `200 OK` with `{"status":"Healthy"}` if the application process is running
- Used by Docker and Kubernetes liveness probes to determine if the container should be restarted
- Performs no external dependency checks

### Readiness Probe
```http
GET /health/ready
```
- Returns `200 OK` when all tagged readiness checks pass (database connectivity, data integrity)
- Returns `503 Service Unavailable` with per-check failure details when any readiness check fails
- Used by Kubernetes readiness probes and load balancers to route traffic only to healthy instances

### Detailed Health
```http
GET /health
```
- Runs all registered health checks with full diagnostic output
- Returns check names, statuses, durations, and exception details
- Suitable for monitoring dashboards and manual diagnostics

## 🚀 Deployment

### Production Considerations

1. **Environment Variables**: Set production values for JWT tokens and connection strings
2. **Database**: Use a production SQL Server instance
3. **HTTPS**: Ensure HTTPS is properly configured
4. **Logging**: Configure appropriate logging providers
5. **CORS**: Update `CorsSettings__AllowedOrigins` environment variable for production domains
6. **Health Checks**: The Docker image includes a `HEALTHCHECK` instruction targeting `/health/live`; configure Kubernetes liveness and readiness probes using the endpoints above

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 👥 User Management System

The system provides comprehensive user management capabilities with unified endpoints for all user types (Student, Instructor, Admin).

### User Management Endpoints

#### User Listing
```http
GET /api/users?status=Active
Authorization: Admin only (AdminPolicy)
```
- **Query Parameters**: `status` (optional) - Filter by `Active`, `Archived`, or `All` (default: `Active`)
- **Response**: List of users with role information and profile details

#### User Deletion Operations

##### Soft Delete (Reversible)
```http
PATCH /api/users/{userId}/soft-delete
Authorization: Admin only (AdminPolicy)
```
- Marks user as deleted but preserves data for potential restoration
- Automatically revokes all active tokens
- Sets `IsDeleted = true` and `DeletedAt` timestamp

##### Hard Delete (Permanent)
```http
DELETE /api/users/{userId}
Authorization: Admin only (AdminPolicy)
```
- Permanently removes user and all associated data
- Cannot be undone
- Includes cascade deletion of related records

##### Restore Soft-Deleted User
```http
PATCH /api/users/{userId}/restore
Authorization: Admin only (AdminPolicy)
```
- Restores a previously soft-deleted user
- Resets `IsDeleted = false` and clears `DeletedAt`
- User regains access to the system

### Supported User Types
- **Students**: Complete profile with enrollment information
- **Instructors**: Teaching assignments and course management
- **Admins**: Full system administration capabilities

### Security Features
- **Self-deletion prevention**: Users cannot delete themselves
- **Role-based authorization**: Only admins can manage users
- **Audit logging**: All operations are logged for compliance
- **Token revocation**: Automatic token cleanup on deletion
- **Data integrity**: Proper cascade handling of related data

### Migration from Legacy Endpoints
The old `DELETE /api/account/admin/users/{userId}` endpoint is deprecated but still functional. New implementations should use the unified UserController endpoints.

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
   - Update `CorsSettings__AllowedOrigins` in your `.env` file for your frontend URL
   - Ensure credentials are included if using cookies

### Getting Help

- Check the application logs for detailed error messages
- Review the Swagger documentation for API usage
- Ensure all prerequisites are properly installed

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
