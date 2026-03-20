# Project Overview

The Attendance Management System is a comprehensive ASP.NET Core 10.0 Web API designed to streamline attendance tracking for educational institutions. This document provides a high-level overview of the system's purpose, features, and architecture.

## System Purpose

The Attendance Management System addresses the challenges of traditional attendance tracking by providing:

- **Digital Attendance Tracking**: QR code-based attendance system
- **Multi-Role Management**: Support for students, instructors, and administrators
- **Academic Structure Management**: Courses, sections, subjects, and schedules
- **Real-time Session Management**: Live attendance sessions with instant feedback
- **Comprehensive Reporting**: Attendance analytics and reporting capabilities

## Key Features

### 🔐 Authentication & Authorization
- **Multi-Authentication Support**: JWT tokens for mobile apps, HTTP-only cookies for web
- **Role-Based Access Control**: Student, Teacher, and Admin roles with specific permissions
- **Secure Token Management**: Refresh token rotation and blacklisting system
- **Identity Integration**: ASP.NET Core Identity for user management

### 👥 User Management
- **Student Profiles**: Regular and irregular student support with flexible enrollment
- **Instructor Management**: Teacher profiles with subject assignments
- **Admin Controls**: System administration and user management
- **Soft Delete**: Safe deletion with restoration capabilities

### 🏫 Academic Structure
- **Course Management**: Academic programs and degree courses
- **Section Organization**: Class sections within courses
- **Subject Catalog**: Individual subjects with codes and descriptions
- **Flexible Scheduling**: Time-based class schedules with room assignments

### 📱 QR Code System
- **Dynamic QR Generation**: Time-limited QR codes for each session
- **Security Features**: Hash-based validation and expiration controls
- **Usage Tracking**: Monitor QR code scans and prevent abuse
- **Revocation System**: Instant QR code deactivation with audit trail

### 📊 Attendance Tracking
- **Session Management**: Start, end, and cancel attendance sessions
- **Real-time Check-ins**: Instant attendance recording via QR codes
- **Status Tracking**: Present, Late, Excused, and Absent status management
- **Manual Override**: Instructor ability to manually mark attendance

### 🏢 Classroom Management
- **Room Inventory**: Physical classroom information and capacity
- **Resource Tracking**: Projector and whiteboard availability
- **Schedule Conflicts**: Prevent double-booking of classrooms
- **Flexible Assignments**: Support for room changes during sessions

## Technology Stack

### Backend Framework
- **ASP.NET Core 10.0**: Modern, cross-platform web framework
- **Entity Framework Core**: Object-relational mapping with SQL Server
- **C# 12**: Latest language features and performance improvements

### Database
- **SQL Server**: Primary database with full ACID compliance
- **Entity Framework Migrations**: Version-controlled schema management
- **Indexing Strategy**: Optimized for common query patterns

### Authentication
- **JWT Bearer Tokens**: Stateless authentication for mobile clients
- **HTTP-Only Cookies**: Secure web authentication
- **ASP.NET Core Identity**: User management and password hashing
- **Refresh Token System**: Secure token renewal mechanism

### API Documentation
- **OpenAPI/Swagger**: Interactive API documentation
- **Scalar UI**: Modern API documentation interface
- **XML Documentation**: Comprehensive endpoint documentation

### Testing
- **xUnit**: Unit testing framework
- **Moq**: Mocking framework for isolated testing
- **Integration Tests**: End-to-end API testing

### Additional Libraries
- **QRCoder**: QR code generation library
- **DotNetEnv**: Environment variable management
- **Serilog**: Structured logging (configurable)

## System Architecture

### Clean Architecture Pattern
The system follows clean architecture principles with clear separation of concerns:

```
Controllers → Services → Repositories → Database
     ↓           ↓           ↓
   DTOs    Business Logic  Data Access
```

### Layer Responsibilities
- **Controllers**: HTTP request handling and response formatting
- **Services**: Business logic and domain operations
- **Repositories**: Data access and persistence
- **Models**: Entity classes and data transfer objects

### Design Patterns
- **Repository Pattern**: Abstracted data access
- **Dependency Injection**: Loose coupling and testability
- **Factory Pattern**: Complex object creation
- **Strategy Pattern**: Multiple authentication methods

## Core Entities

### User Management
- **IdentityUser**: ASP.NET Core Identity base user
- **Student**: Student-specific profile information
- **Instructor**: Teacher profile and assignments
- **Admin**: Administrative user information

### Academic Structure
- **Course**: Academic programs (e.g., Computer Science)
- **Section**: Class sections within courses (e.g., CS-A, CS-B)
- **Subject**: Individual subjects/courses (e.g., Data Structures)
- **Classroom**: Physical room information and resources

### Scheduling & Sessions
- **Schedule**: Recurring class schedules
- **Session**: Individual class session instances
- **AttendanceRecord**: Student attendance for specific sessions
- **QrCode**: Generated QR codes for attendance tracking

### Enrollment System
- **StudentEnrollment**: Flexible student-subject enrollment
- **Regular Students**: Enrolled in all section subjects
- **Irregular Students**: Custom subject enrollment

## Key Workflows

### Student Registration Flow
1. User registers with student role
2. Student profile created with section assignment
3. Automatic enrollment in section subjects (regular students)
4. Manual enrollment for additional subjects (irregular students)

### Attendance Session Flow
1. Instructor starts session for scheduled class
2. System generates time-limited QR code
3. Students scan QR code to mark attendance
4. System records attendance with timestamp
5. Instructor ends session and finalizes attendance

### QR Code Security Flow
1. QR code generated with unique hash
2. Hash includes session, timestamp, and security data
3. QR code expires after configurable time
4. Scanned codes validated against database
5. Used codes tracked to prevent replay attacks

## Security Features

### Authentication Security
- **Password Hashing**: ASP.NET Core Identity with secure hashing
- **JWT Security**: Signed tokens with expiration
- **Token Blacklisting**: Immediate token revocation
- **Refresh Token Rotation**: Enhanced security for long-lived sessions

### Data Protection
- **Soft Deletes**: Preserve data integrity
- **Audit Trails**: CreatedAt/UpdatedAt timestamps
- **Input Validation**: Comprehensive request validation
- **SQL Injection Prevention**: Parameterized queries via EF Core

### API Security
- **HTTPS Enforcement**: Secure communication
- **CORS Configuration**: Cross-origin request control
- **Rate Limiting**: Prevent API abuse
- **Error Handling**: Secure error responses without sensitive data

## Performance Features

### Database Optimization
- **Strategic Indexing**: Optimized for common queries
- **Eager Loading**: Efficient related data loading
- **Query Optimization**: AsNoTracking for read-only operations
- **Connection Pooling**: Efficient database connections

### Caching Strategy
- **Response Caching**: Cache static or semi-static responses
- **Memory Caching**: In-memory cache for frequently accessed data
- **Query Result Caching**: EF Core query caching

### Background Services
- **Token Cleanup**: Automatic expired token removal
- **Scheduled Tasks**: Maintenance operations
- **Async Operations**: Non-blocking I/O operations

## Scalability Considerations

### Horizontal Scaling
- **Stateless Design**: No server-side session state
- **Database Scaling**: Read replicas and partitioning support
- **Load Balancer Ready**: No sticky session requirements

### Vertical Scaling
- **Efficient Memory Usage**: Optimized object allocation
- **CPU Optimization**: Async/await patterns
- **I/O Optimization**: Efficient database queries

## Integration Capabilities

### API-First Design
- **RESTful APIs**: Standard HTTP methods and status codes
- **JSON Communication**: Structured data exchange
- **OpenAPI Specification**: Machine-readable API documentation

### External System Integration
- **Webhook Support**: Event-driven integrations
- **Bulk Data Operations**: Import/export capabilities
- **Third-party Authentication**: Extensible authentication providers

## Monitoring & Observability

### Logging
- **Structured Logging**: JSON-formatted logs
- **Log Levels**: Configurable logging verbosity
- **Request Tracing**: Track requests across components

### Health Monitoring
- **Health Check Endpoints**: System status monitoring
- **Database Health**: Connection and query health
- **Performance Metrics**: Response time and throughput

### Error Handling
- **Global Exception Handling**: Consistent error responses
- **Error Logging**: Detailed error information for debugging
- **User-Friendly Messages**: Clean error messages for clients

This project overview provides the foundation for understanding the Attendance Management System's capabilities, architecture, and design principles.
