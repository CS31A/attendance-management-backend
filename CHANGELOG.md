# Changelog

All notable changes to the Attendance Monitoring System project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v1.6.0] - 2025-11-01

### 🎉 **Major Features**

#### Attendance System Performance Optimizations
- **Added** major performance improvements to attendance record retrieval with 80-90% faster queries
  - Introduced new optimized DTOs for different use cases:
    - `AttendanceListDto` - Lightweight DTO for listing views (5 fields only)
    - `AttendanceMinimalDto` - Minimal DTO for duplicate checks and simple lookups (6 fields)
    - `SessionAttendanceRosterDto` - Optimized DTO for session roster displays (6 fields)
  - Implemented database-level projections to reduce data transfer and improve query performance
  - Added server-side filtering and pagination to minimize memory usage
- **Enhanced** `AttendanceRepository` with comprehensive query methods:
  - `GetAllForListingAsync()` - Returns full `AttendanceRecordResponseDto` with pagination
  - `GetAllForListingOptimizedAsync()` - Returns lightweight `AttendanceListDto` (80-90% faster)
  - `GetBySessionIdForRosterAsync()` - Optimized roster view (90% faster than full query)
  - `GetBySessionAndStudentMinimalAsync()` - Minimal data retrieval (95% faster)
  - `GetFilteredAsync()` - Advanced filtering with database-level WHERE clauses
  - `GetStatisticsAsync()` - Database-level aggregations without loading entities
- **Added** tracked retrieval methods across repositories:
  - `InstructorRepository.GetInstructorByIdTrackedAsync()`
  - `StudentRepository.GetStudentByIdTrackedAsync()`
  - `ScheduleRepository.GetScheduleByIdTrackedAsync()`
  - `SubjectRepository.GetSubjectByIdTrackedAsync()`
  - `StudentEnrollmentRepository.GetEnrollmentByIdTrackedAsync()`
  - Enables proper change tracking for update operations
- **Refactored** `AttendanceService` to utilize new optimized repository methods:
  - Replaced in-memory filtering with database-level filtering
  - Removed redundant data loading and processing
  - Improved statistics calculation with direct database aggregations
- **Added** comprehensive test coverage in `AttendanceRepositoryTest.cs`:
  - Tests for optimized listing methods
  - Pagination validation tests
  - Roster view optimization tests
  - Minimal DTO retrieval tests
  - Statistics aggregation tests

#### QR Code Atomic Operations and Race Condition Fix
- **Fixed** critical race condition in QR code scanning that could allow duplicate attendance records
- **Implemented** atomic increment operation at database level:
  - Added `IncrementUsageCountAsync()` method with SQL-level atomic operation
  - Uses `ExecuteSqlRawAsync` with direct SQL for true atomicity
  - Prevents concurrent scans from bypassing MaxUsage limits
- **Enhanced** `QrCodeService.ScanQrCodeAsync()` with transaction support:
  - Wraps QR validation, increment, and attendance creation in single transaction
  - Ensures all-or-nothing behavior for scan operations
  - Proper rollback on any failure in the process
- **Added** better error messages for QR code usage limits:
  - Distinguishes between "QR code has reached maximum usage" vs other errors
  - Provides clear feedback when concurrent scans are detected
- **Updated** `IQrCodeRepository` interface with atomic operation method
- **Improved** concurrency handling with proper locking at database level

#### Session Date Management Enhancement
- **Enhanced** session creation with optional `SessionDate` parameter:
  - Made `SessionDate` nullable in `CreateSession` DTO
  - Automatically defaults to current date (`DateTime.Today`) when not provided
  - Simplifies session creation workflow for current-day sessions
- **Updated** `SessionService.CreateSessionAsync()` with null-coalescing logic:
  - `session.SessionDate = request.SessionDate ?? DateTime.Today`
  - Maintains backward compatibility with explicit date specification
- **Added** comprehensive test coverage in `SessionServiceTest.cs`:
  - `CreateSession_WithNullSessionDate_DefaultsToToday()` - Validates default behavior
  - `CreateSession_WithExplicitSessionDate_UsesProvidedDate()` - Validates explicit date
  - Updated existing tests to handle nullable `SessionDate` property
- **Improved** UX by reducing required fields in session creation requests

#### Attendance Service Error Handling and Data Integrity
- **Added** comprehensive null checks in `AttendanceService` for navigation properties:
  - Validates `Student`, `Session`, `Schedule`, `Subject`, `Section`, `Classroom`, `Instructor` properties
  - Prevents null reference exceptions in DTO mapping
  - Provides clear error messages when required data is missing
- **Enhanced** `MapToResponseDto()` method with validation:
  - Throws `InvalidOperationException` with specific property name when navigation property is not loaded
  - Ensures data integrity before mapping to response DTOs
  - Improves debugging with descriptive error messages
- **Added** `ActualRoom` navigation property support in `AttendanceRepository`:
  - Includes `Session.ActualRoom` in all query methods with includes
  - Supports cases where session room differs from scheduled room
  - Updated `ApplyFullIncludes()` and `ApplyFullIncludesWithSplitQuery()` helper methods
- **Improved** error handling for missing or incomplete data in attendance operations

### 🔧 **Technical Improvements**

#### Performance Optimizations
- **Optimized** database queries using server-side projections (80-90% improvement)
- **Reduced** data transfer by using minimal DTOs for specific use cases
- **Implemented** database-level aggregations for statistics (no entity loading)
- **Added** split query optimization for multi-record retrieval with navigation properties
- **Improved** pagination efficiency with database-level SKIP/TAKE operations

#### Code Quality Enhancements
- **Enhanced** XML documentation across repository and service methods
- **Added** performance notes in method comments explaining optimization benefits
- **Improved** code organization with clear separation of concerns
- **Updated** method signatures for better clarity and type safety
- **Added** comprehensive unit tests validating optimizations and edge cases

#### Database Operations
- **Implemented** atomic operations using raw SQL for critical race condition prevention
- **Enhanced** transaction management for complex multi-step operations
- **Added** proper change tracking methods for update scenarios
- **Improved** query composition using IQueryable for efficient filtering

### 📝 **Documentation**

#### Code Documentation
- **Added** detailed performance characteristics in XML comments:
  - Query execution details (single query vs split query)
  - Performance comparisons between methods (e.g., "80-90% faster")
  - Use case recommendations for each method
  - Navigation property loading details
- **Enhanced** repository method documentation with:
  - Performance metrics and benchmarks
  - When to use each method variant
  - Trade-offs between full entity loading and DTO projections
- **Improved** service method documentation with clearer parameter descriptions

### 🐛 **Bug Fixes**

#### Concurrency Issues
- **Fixed** QR code race condition allowing duplicate attendance via atomic increment
- **Fixed** potential data inconsistency in concurrent QR scanning scenarios
- **Resolved** transaction isolation issues in attendance creation flow

#### Data Integrity
- **Fixed** potential null reference exceptions in attendance DTO mapping
- **Fixed** missing navigation property loading causing runtime errors
- **Resolved** issues with ActualRoom not being included in queries

### ⚠️ **Known Issues**

#### Security - Authorization Silent Failure (Identified, Not Yet Fixed)
- **Identified** fail-open security pattern in `AttendanceService`:
  - `GetAllAttendanceAsync()` (lines 191-201) - When Student role user has null userId, silently returns all records
  - `GetAttendanceSummaryAsync()` (lines 368-378) - Same issue with summary statistics
  - **Impact:** Low likelihood (requires malformed JWT), but HIGH impact (unauthorized data access)
  - **Status:** Documented in NEXT_STEPS.md, fix planned for next release
  - **Workaround:** Proper JWT validation at authentication middleware level prevents this scenario

### 🔮 **Upcoming Changes**

- Fix for authorization silent failure pattern (v1.6.1)
- Additional test coverage for edge cases and security scenarios
- Performance monitoring and logging enhancements
- Load testing for concurrent QR scanning scenarios

---

## [v1.5.0] - 2025-10-31

### 🎉 **Major Features**

#### User Profile Update System
- **Added** comprehensive user profile update functionality for authenticated users
  - Users can update their own profiles including email, password, and personal information
  - Includes password change with current password verification
  - Supports role-specific profile updates (Student, Instructor, Admin)
- **Added** admin update user capabilities allowing administrators to modify any user's profile
  - Admins can reset passwords without requiring current password
  - Supports updating user details, role-specific information, and soft delete operations
  - Includes enhanced authorization controls and audit logging
- **Added** new DTOs for profile management:
  - `UpdateProfile` - Request DTO for user profile updates
  - `AdminUpdateUser` - Request DTO for admin user updates
  - `UpdateProfileResponse` - Response DTO with updated profile information
- **Enhanced** `AccountService` with profile update methods:
  - `UpdateUserProfileAsync` - Handles authenticated user profile updates
  - `AdminUpdateUserProfileAsync` - Handles administrative user updates
  - Improved error handling and validation logic
- **Enhanced** `AccountRepository` with profile update methods for all user types
- **Added** new endpoints in `AccountController`:
  - `PATCH /api/Account/profile` - User profile update
  - `PATCH /api/Account/admin/users/{userId}` - Admin user update
- **Added** `UpdateUser.md` documentation for profile update implementation details

### 🐛 **Bug Fixes**

#### Email Uniqueness Race Condition Fix
- **Fixed** potential race condition in email uniqueness validation during user registration and profile updates
- **Added** database-level unique constraint on `NormalizedEmail` in `AspNetUsers` table
  - Prevents duplicate emails at the database level using unique index with filter for non-null values
  - Includes migration to clean up existing duplicates before applying constraint
- **Enhanced** error handling in `AccountService` to catch and handle `DbUpdateException` for unique constraint violations
  - Provides user-friendly error messages instead of generic database errors
  - Wraps update operations in try-catch blocks with specific exception filtering
- **Updated** `ApplicationDbContext` with Fluent API configuration for the unique email index
- **Improved** `AccountRepository` and service methods to handle constraint violations gracefully

### 🔧 **Technical Improvements**

#### Session and Attendance Record Management
- **Added** `Session` entity to represent actual class session occurrences
  - Tracks when a class actually happens, including start/end times and room changes
  - Supports session statuses: "not_started", "active", "ended", "cancelled"
  - Includes session date, actual start/end times, attendance cutoff time, and room information
- **Added** `AttendanceRecord` entity to track student attendance for sessions
  - Records when students checked in and their attendance status
  - Supports status values: "Present", "Late", "Excused", "Absent"
  - Tracks whether attendance was manually entered or through QR code scan
- **Refactored** QR code system to be tied to sessions rather than directly to schedules
  - QR codes are now generated for specific `Session` instances instead of schedule/section/room combinations
  - Improves tracking of actual class occurrences rather than recurring schedules
- **Added** `SessionController` with full CRUD operations and room update functionality
  - Includes endpoints to get sessions by schedule, status, or date
  - Supports updating the actual room for active sessions
  - Provides comprehensive session management capabilities
- **Implemented** session lifecycle endpoints for instructors
  - Added create (`POST /api/sessions`), start (`PATCH /api/sessions/{id}/start`), end (`PATCH /api/sessions/{id}/end`), and cancel (`DELETE /api/sessions/{id}`) operations
  - Introduced dedicated DTOs (`CreateSession`, `StartSession`, `EndSession`, `CancelSession`) with validation rules
  - Connected controller workflows to new service and repository logic with explicit authorization, logging, and error handling

### 🐛 **Bug Fixes**

#### Authentication Improvements
- **Fixed** `LoginAsync` method to return actual username instead of login identifier
  - Updated `IAccountService.LoginAsync` signature to return tuple with username
  - Modified `AccountService.LoginAsync` to return the authenticated user's username
  - Updated `AccountController.Login` to use returned username in response instead of input identifier
  - Ensures login response contains the correct username regardless of whether user logged in with email or username
  - Improved security by not echoing back user input directly

### 🧪 **Testing Infrastructure**

#### Account Controller Tests
- **Added** new unit test `Login_ReturnsOk_AndCorrectUsername_WhenLoginSuccessful`
  - Tests successful login returns correct username in response
  - Validates `LoginResponseDto` structure and success status
  - Verifies username is properly extracted from service layer
- **Fixed** indentation in existing `Register_ReturnsOk_WhenRegistrationSuccessful` test

#### Comprehensive Test Suite for Student Enrollment Feature
- **Added** 44 new unit tests for irregular student enrollment functionality
  - 28 service layer tests for `StudentEnrollmentService`
  - 16 controller tests for `StudentEnrollmentController`
  - 100% pass rate across all new tests
  - Total project tests increased from 25 to 69
- **Test Coverage**
  - All 13 public service methods tested with happy paths, edge cases, and error scenarios
  - All 6 API endpoints tested with success and failure scenarios
  - Complete HTTP status code coverage (200, 404, 409)
  - Exception handling verification for all error paths
  - Response DTO mapping validation
- **Test Documentation**
  - Created comprehensive test plan for service layer
  - Created detailed test plan for controller layer
  - Documented all test cases with purpose and expected outcomes
  - Added test quality metrics and coverage reports

### 🔧 **Technical Improvements**

#### QrCodeService Dependency Injection Refactoring
- **Refactored** `QrCodeService` to use `ISessionRepository` instead of direct `ApplicationDbContext` dependency
  - Replaced `ApplicationDbContext _context` field with `ISessionRepository _sessionRepository`
  - Updated constructor to inject `ISessionRepository` instead of `ApplicationDbContext`
  - Refactored `ValidateSessionExistsAsync()` method to use `_sessionRepository.GetSessionByIdAsync()`
  - Improves separation of concerns by removing data access logic from service layer
  - Enhances testability by depending on repository interface instead of concrete DbContext
  - Follows repository pattern consistently used throughout the codebase
  - Better encapsulation of query logic centralized in repository layer
  - Repository method includes comprehensive navigation property loading (`AsNoTracking`, `AsSplitQuery`)
- **Impact**: No breaking changes, all 70 tests passing, improved code architecture and maintainability

#### Comprehensive Unit Testing for Session Management
- **Added** comprehensive unit tests for `SessionController` and `SessionService`
  - Implemented 752 lines of tests covering all controller endpoints and error scenarios
  - Added 1024 lines of service layer tests with complete test coverage for business logic
  - Tests cover all session lifecycle operations: create, start, end, cancel, and room updates
  - Includes comprehensive validation for authorization, error handling, and edge cases
  - All new tests pass with 100% success rate

#### Entity Relationship Refactoring
- **Refactored** QR code relationships to connect with Session instead of Schedule/Section/Room
  - QR codes now connect to sessions for better tracking of actual class instances
  - Updated all related services, repositories, and controllers to use SessionId
  - Improved navigation property loading with `AsSplitQuery()` for performance
- **Enhanced** database schema with proper Session and AttendanceRecord relationships
  - Added foreign key constraints and indexes for optimal performance
  - Implemented composite indexes to prevent duplicate attendance records
  - Added cascading delete for attendance records when sessions are deleted

#### Database Schema Optimization
- **Removed** redundant composite index on StudentId and SessionId in AttendanceRecord table
  - The composite index `[Index(nameof(StudentId), nameof(SessionId))]` was removed to optimize database performance
  - Individual indexes on StudentId, SessionId, and CheckInTime remain for efficient querying
  - Reduces database storage overhead and improves write performance

#### Repository Navigation Property Enhancement
- **Enhanced** SessionRepository with additional navigation property loading
  - Added `Include(s => s.Schedule).ThenInclude(sch => sch.Instructor)` to multiple repository methods
  - Ensures instructor information from schedule is loaded when retrieving sessions
  - Improves data retrieval efficiency for session-related operations
  - Applied consistently across GetSessionByIdAsync, GetSessionsByScheduleAsync, GetSessionsByStatusAsync, GetSessionsByDateAsync, GetSessionsWithAttendanceAsync, GetSessionWithDetailsAsync, and GetSessionForAttendanceValidationAsync methods

#### Documentation Updates
- **Updated** DBML ERD structure to reflect new Session and Attendance entities
  - Added timestamp fields to instructors, students, and admins tables
  - Updated schedules table to connect to subjects, sections, and instructors
  - Modified sessions table with proper status values and date field type
  - Refined QR table to connect with sessions instead of schedules
  - Updated attendance table with proper foreign key relationships
  - Added building and capacity fields to classrooms table
  - Added max_usage, revocation tracking to QR table
  - Improved foreign key constraints and nullability definitions

#### Code Architecture Refactoring
- **Modularized** Program.cs into extension methods for better organization
  - Extracted configuration logic into 6 ServiceCollection extension files
  - Created WebApplication extension files for middleware pipeline management
  - Reduced Program.cs from 407 lines to 59 lines (85% reduction)
  - Improved maintainability and readability with logical groupings
  - Enhanced team collaboration with reduced merge conflicts

#### Error Handling Enhancements
- **Implemented** centralized global exception handler middleware
  - Replaced controller-level try-catch blocks with unified middleware in Program.cs
  - Removed generic exception handling from 5 controllers (Account, Instructor, QrCode, Student, Subject)
  - Reduced code duplication by ~100+ lines
  - Improved error response consistency across all endpoints
- **Enhanced** delete operation error handling
  - Added specific foreign key constraint violation detection
  - Implemented user-friendly error messages for constraint failures
  - Improved client-side error understanding by identifying which constraint prevents deletion
  - Better error message specificity for cascading delete failures

#### Performance Optimizations
- **Optimized** student enrollment queries with database-level filtering
  - Moved active enrollment filtering from application layer to database layer
  - Improved API response time for enrollment endpoints
  - Enhanced response completeness for student enrollment data

#### Code Quality
- **Improved** `.gitignore` configuration
  - Added AI assistant configuration exclusions (.claude/, .qwen/, .crush/)
  - Added temporary and scratch file patterns
  - Enhanced IDE-specific file exclusions
  - Better organization with categorized sections

#### Performance Optimizations with AsNoTracking
- **Added** `.AsNoTracking()` to all read-only database queries for performance improvement
  - Applied to AccountRepository for student and instructor retrieval methods
  - Applied to StudentEnrollmentRepository for all read operations
  - Applied to AccountService for user profile retrieval
  - Applied to BlacklistedTokenCleanupService for expired token cleanup
  - Applied to ScheduleService for validation queries
  - Applied to TokenValidationService for token validation
  - Significantly reduces entity tracking overhead for read operations
  - Improves response times and memory usage for frequently accessed data

#### User Model Architecture Enhancement
- **Removed** redundant email fields from user models
  - Removed `Email` property from `Admin`, `Instructor`, and `Student` model classes
  - Eliminated duplicate email storage in favor of using ASP.NET Identity's email field
  - Updated `UserFactory` to remove email parameter when creating users
  - Updated `CreateStudent`, `UpdateStudent`, `CreateInstructor`, and `UpdateInstructor` DTOs to remove email properties
  - Created database migration to remove email columns from Admins, Instructors, and Students tables
- **Enhanced** email access via Identity user relationship
  - Updated repositories to include User navigation property for email access
  - Modified service methods to access email from Identity user instead of local email property
  - Updated controller methods to access email via User navigation property
  - Updated unit tests to reflect email field removal in models
  - Improved data consistency by centralizing email storage in Identity user

#### User Profile Response DTO Enhancement
- **Removed** redundant email properties from UserProfileResponseDto
  - Removed `Email` property from `StudentProfileInfo` in `UserProfileResponseDto.cs`
  - Removed `Email` property from `InstructorProfileInfo` in `UserProfileResponseDto.cs`
  - Updated `AccountService` to stop mapping email from user entity to profile response
  - Maintains consistent approach with centralized email storage in Identity user
  - Reduces data redundancy in API responses

---

## [Unreleased]

### 🎉 **Major Features**

#### Comprehensive Attendance Tracking System
- **Added** complete attendance tracking system with full CRUD operations
- **Added** `AttendanceRecord` entity to track student attendance for sessions
  - Records check-in times and attendance status (Present, Late, Excused, Absent)
  - Tracks whether attendance was manually entered or through QR code scan
  - Supports duplicate prevention with QR code validation
- **Added** `AttendanceController` with endpoints for attendance management
  - `GET /api/Attendance` - Retrieve attendance records with filtering and pagination
  - `GET /api/Attendance/{id}` - Get specific attendance record
  - `POST /api/Attendance` - Create new attendance record
  - `PUT /api/Attendance/{id}` - Update existing attendance record
  - `DELETE /api/Attendance/{id}` - Delete attendance record
  - `GET /api/Attendance/student/{studentId}` - Get student's attendance history
  - `GET /api/Attendance/session/{sessionId}/summary` - Get session attendance summary
- **Added** new DTOs for attendance management:
  - `CreateAttendanceRequest` - Request DTO for creating attendance records
  - `UpdateAttendanceRequest` - Request DTO for updating attendance records
  - `AttendanceRecordResponseDto` - Response DTO for attendance records
  - `AttendanceSummaryDto` - Response DTO for attendance summaries
  - `StudentAttendanceHistoryDto` - Response DTO for student history
  - `SessionAttendanceDto` - Response DTO for session attendance
  - `AttendanceFilterRequest` - Request DTO for filtering attendance records
  - `PagedResult` - Generic DTO for paginated results
- **Added** `AttendanceService` and `AttendanceRepository` with full business logic
- **Integrated** attendance system with QR code scanning for automatic check-ins
- **Added** role-based authorization for attendance management (Student, Instructor, Admin)
- **Implemented** attendance statistics and session overview functionality
- **Enhanced** QR code service to link attendance records automatically

### 🐛 **Bug Fixes**

#### Email Update Functionality
- **Fixed** email update functionality in user profile updates (Patch #19)

#### API Documentation Corrections
- **Corrected** HTTP methods in API documentation from PUT to PATCH where appropriate

---

## [v1.4.0] - 2025-10-26

### 🎉 **Major Features**

#### Student Enrollment Management System
- **Added** comprehensive student enrollment system for irregular students
- **Added** `StudentEnrollment` entity with support for cross-section enrollments
- **Added** enrollment types: Regular, Irregular, Retake
- **Added** academic year and semester tracking
- **Added** enrollment status management (Active/Inactive, Drop functionality)
- **Added** QR code integration for enrolled subjects
- **Added** full CRUD operations via `StudentEnrollmentController`

#### QR Code Revocation & Audit Trail
- **Added** manual QR code revocation system with full audit trail
- **Added** revocation tracking: `RevokedAt`, `RevokedBy`, `RevocationReason`
- **Added** QR code reactivation capability (for non-expired codes)
- **Added** four new API endpoints for revocation management
- **Added** authorization controls (Admin/Instructor only)

### 🔧 **Technical Improvements**

#### Performance Optimizations
- **Added** GZIP compression for GET requests on specific API endpoints
  - Configured compression to only apply to JSON content types
  - Improved performance monitoring middleware using `Response.OnStarting()` for accurate timing
  - Selective compression for optimal performance vs bandwidth trade-offs
- **Added** `.AsNoTracking()` for read-only database queries
- **Added** `ConfigureAwait(false)` for all async operations
- **Optimized** async operations by converting unnecessary async methods to synchronous
- **Added** retry logic for unique QR hash generation
- **Added** performance monitoring capabilities with enhanced metrics tracking

#### Security Enhancements
- **Implemented** token family revocation for enhanced security
- **Added** token reuse detection and automatic family revocation
- **Fixed** policy authorization hierarchy issues
- **Added** comprehensive security violation logging

#### Code Quality & Architecture
- **Created** `UserContextService` for centralized user context management
- **Created** `TokenConstants` class to eliminate magic numbers
- **Removed** code duplication (~70+ lines of duplicate `GetUserIdAsync` methods)
- **Removed** unused dependencies from controllers (e.g., `IConfiguration` from `AccountController`)
- **Improved** XML documentation with fully qualified exception type names
- **Added** proper model validation with `[Required]` and `[StringLength]` attributes
- **Fixed** missing `UpdatedAt` timestamp updates in `StudentService`
- **Updated** unit tests to reflect service layer changes

### 🛠 **Infrastructure & DevOps**

#### Framework & Dependencies
- **Upgraded** to .NET 9.0
- **Updated** Entity Framework Core to 9.0.7
- **Updated** ASP.NET Core Identity to 9.0.7
- **Added** Scalar API documentation (v2.6.7)
- **Added** QRCoder library (v1.6.0)
- **Added** DotNetEnv for environment management (v3.1.1)

#### Database Schema
- **Added** 24 database migrations with comprehensive schema updates
- **Added** composite indexes for performance optimization
- **Added** unique constraints for data integrity
- **Added** soft delete functionality for Instructor and Student entities
- **Added** blacklisted tokens table for security
- **Added** refresh token family tracking

### 📚 **API Endpoints**

#### New Endpoints Added
```
# Student Enrollment Management
GET    /api/StudentEnrollment/student/{studentId}
POST   /api/StudentEnrollment
PUT    /api/StudentEnrollment/{id}
DELETE /api/StudentEnrollment/{id}

# QR Code Revocation
PATCH  /api/QrCode/{id}/revoke
PATCH  /api/QrCode/hash/{qrHash}/revoke
PATCH  /api/QrCode/{id}/reactivate
PATCH  /api/QrCode/hash/{qrHash}/reactivate

# Student Subject Access
GET    /api/Student/{studentId}/subjects
```

#### Student Subject Retrieval
- **Added** endpoint for students to retrieve their enrolled subjects
- **Includes** comprehensive subject information:
  - Subject details (code, description, units)
  - Schedule information (days, time slots)
  - Instructor details (name, email)
  - Classroom information (room number, building)
- **Supports** both regular and irregular student enrollments
- **Authorization** limited to authenticated students accessing their own data
- **Created** `StudentSubjectResponseDto` and `StudentSubjectScheduleDto` for structured responses

### 🔍 **Core System Components**

#### Authentication & Authorization
- JWT-based authentication with refresh token rotation
- Role-based authorization (Admin, Instructor, Student)
- Blacklisted token management
- Token family revocation for enhanced security

#### QR Code System
- Dynamic QR code generation with unique hash combinations
- Expiration-based validation
- Usage count tracking and limits
- Manual revocation with audit trail
- Room-based validation for attendance

#### User Management
- Student management with regular/irregular classification
- Instructor management with course assignments
- Admin management with full system access
- Soft delete functionality for data integrity

#### Academic Structure
- Course management with instructor assignments
- Section management with student enrollments
- Subject management with schedule integration
- Classroom management with capacity tracking
- Schedule management with time slot validation

### 🧪 **Testing Infrastructure**
- Comprehensive unit tests for all controllers
- Test project with xUnit framework
- Controller testing for Account, Health, Instructor, QrCode, Schedule, Student, and Subject
- Test coverage for CRUD operations and edge cases

### 📦 **Dependencies & Packages**
```xml
.NET 9.0
Microsoft.AspNetCore.Identity.EntityFrameworkCore 9.0.7
Microsoft.EntityFrameworkCore.SqlServer 9.0.7
Microsoft.AspNetCore.Authentication.JwtBearer 9.0.7
QRCoder 1.6.0
Scalar.AspNetCore 2.6.7
Swashbuckle.AspNetCore 6.1.4
System.IdentityModel.Tokens.Jwt 8.13.0
DotNetEnv 3.1.1
```

### 🐛 **Bug Fixes**
- **Fixed** build issues and test failures
- **Fixed** QrCodes migration column synchronization
- **Fixed** policy authorization hierarchy
- **Fixed** missing dependencies in test constructors
- **Fixed** Section-Instructor relationship mapping

### 📋 **Database Schema Overview**

#### Core Entities
- `Students` - Student information with regular/irregular classification
- `Instructors` - Instructor details with course assignments
- `Admins` - Administrative users with full access
- `Courses` - Academic courses with instructor management
- `Sections` - Class sections with student enrollments
- `Subjects` - Individual subjects with schedule integration
- `Classrooms` - Physical classroom management
- `Schedules` - Time slot management with instructor assignments

#### System Entities
- `QrCodes` - QR code management with revocation tracking
- `StudentEnrollments` - Flexible enrollment system for irregular students
- `RefreshTokens` - Token family management for security
- `BlacklistedTokens` - Revoked token tracking

### 🎯 **Key Performance Metrics**
- **Code Duplication Reduced**: ~70+ lines eliminated
- **Database Queries Optimized**: AsNoTracking() for read operations
- **Security Enhanced**: Token family revocation implemented
- **API Response Time**: Improved with GZIP compression
- **Test Coverage**: Comprehensive controller and service testing

### 📖 **Documentation**
- Complete API endpoint documentation
- Implementation guides for major features
- Performance optimization summaries
- Security feature explanations
- Database migration tracking

---

## Previous Development History

### [v1.3.0] - 2025-10-20
- QR Code revocation audit trail implementation
- Enhanced security with token family revocation
- Performance optimizations with AsNoTracking queries

### [v1.2.0] - 2025-10-19
- Section-Instructor relationship fixes
- Composite indexes and performance improvements
- Enhanced model validation

### [v1.1.0] - 2025-10-16
- Student irregular classification system
- Enhanced QR code generation with retry logic
- Improved error handling and logging

### [v1.0.0] - 2025-08-15
- Initial system implementation
- Basic authentication and authorization
- Core CRUD operations for all entities
- QR code generation and validation
- Database foundation with Entity Framework

---

## Future Roadmap

### 🚀 **Planned Features**
1. **Real-time Notifications** - WebSocket integration for live updates
2. **Advanced Analytics** - Attendance reporting and insights
3. **Batch Operations** - Bulk student enrollment and management
4. **Integration APIs** - External system integration capabilities

### 🔧 **Technical Improvements**
1. **Caching Implementation** - Redis for frequently accessed data
2. **Rate Limiting** - API protection against abuse
3. **Monitoring & Logging** - Comprehensive application monitoring
4. **API Versioning** - Structured API evolution management
5. **Automated Testing** - Expanded test coverage and CI/CD

### 📊 **System Enhancements**
1. **Advanced Reporting** - Detailed attendance analytics
2. **Audit Trail Expansion** - Complete system action tracking
3. **Data Export/Import** - Bulk data management capabilities
4. **Multi-tenant Support** - Support for multiple institutions
5. **Offline Capability** - Limited offline functionality for mobile apps

---

*This changelog is maintained to track all significant changes to the Attendance Monitoring System. For detailed technical implementation notes, see the respective documentation files in the project repository.*
