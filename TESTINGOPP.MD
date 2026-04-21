# Testing Opportunities

## Current State

- The repository already has a solid unit-test base in `attendance.testproject/` with xUnit and Moq.
- There are existing focused tests for some controllers and services, plus a few database and transaction checks.
- Integration coverage is still limited. The main HTTP-style integration coverage currently found is `attendance.testproject/Integration_Testing/GlobalExceptionHandlerIntegrationTests.cs`, and the repo does not yet use `WebApplicationFactory`.
- `attendance.testproject/attendance.testproject.csproj` already includes useful packages for expanding coverage, including `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.EntityFrameworkCore.Sqlite`.

## Highest-Value Unit Test Opportunities

### 1. Authentication and refresh token flows

**Files**
- `attendance_monitoring/Services/Account/AuthenticationService.cs`
- `attendance_monitoring/Services/RefreshTokenService.cs`

**Why this is high value**
- These classes contain security-sensitive branching and token lifecycle logic.
- A failure here affects login, refresh, logout, and token reuse handling across the whole app.

**Recommended tests**

For `AuthenticationService.LoginAsync`:
- logs in with email identifier when the user exists by email
- logs in with username identifier when the user exists by username
- throws `ValidationException` when user is missing
- throws `ValidationException` when password check fails
- throws `ValidationException` when the user has no assigned role

For `AuthenticationService.RefreshAsync`:
- returns new access and refresh tokens for a valid refresh request
- throws `EntityNotFoundException<string>` when the refresh token points to a missing user
- blacklists the old access token only when it belongs to the same user and is still valid
- skips blacklisting when old access token is missing or invalid but still rotates refresh token successfully

For `RefreshTokenService.ValidateRefreshTokenAsync`:
- throws when token hash is not found
- throws when token is expired
- throws and revokes the token family when a revoked token is reused

For `RefreshTokenService.RotateRefreshTokenAsync`:
- revokes the old token, links replacement token hash, and persists changes
- wraps unexpected repository failures in a service exception

### 2. Registration validation and profile creation handoff

**File**
- `attendance_monitoring/Services/Account/RegistrationService.cs`

**Why this is high value**
- This is a dense validation seam with role-specific behavior and repository lookups.
- It is only partially covered today through `attendance.testproject/Services_Testing/RoleStandardizationTests.cs`.

**Recommended tests**
- throws when passwords do not match
- throws when username already exists
- throws when email already exists
- throws when role is invalid
- throws when a non-student request includes `SectionId`
- throws when a student request omits `SectionId`
- throws when student `SectionId` does not exist
- throws when student `Firstname` or `Lastname` is missing
- returns success when `IUserFactory.CreateUserAsync` succeeds
- throws `EntityServiceException` when `IUserFactory.CreateUserAsync` returns failure

### 3. Schedule business validation

**File**
- `attendance_monitoring/Services/ScheduleService.cs`

**Why this is high value**
- `CreateScheduleAsync` and `UpdateScheduleAsync` combine time validation, day-of-week validation, and relationship existence checks.
- I found controller tests around schedules, but no direct service tests for this business logic.

**Recommended tests**
- `CreateScheduleAsync` rejects invalid `DayOfWeek`
- `CreateScheduleAsync` rejects `TimeOut <= TimeIn`
- `CreateScheduleAsync` rejects missing subject/classroom/section/instructor
- `CreateScheduleAsync` returns created schedule when inputs are valid
- `UpdateScheduleAsync` rejects invalid partial updates that produce an invalid time range
- `UpdateScheduleAsync` rejects missing related entities when IDs are changed
- `UpdateScheduleAsync` updates only provided fields and preserves existing values for omitted fields
- `UpdateScheduleAsync` throws when repository update returns null

### 4. Student lifecycle service behavior

**File**
- `attendance_monitoring/Services/StudentService.cs`

**Why this is high value**
- This service contains real branching around user context, authorization, duplicates, delete/restore behavior, and lookup failures.
- Current controller tests do not replace direct service coverage here.

**Recommended tests**

For `CreateStudentAsync`:
- rejects invalid `SectionId`
- rejects nonexistent section
- rejects missing user ID from principal
- throws `EntityAlreadyExistsException<string>` when student already exists for user
- creates and saves a student when data is valid

For `RestoreStudentAsync`:
- returns error message for invalid student ID
- returns error message when caller user ID is missing
- returns error message when student does not exist
- returns unauthorized message when caller is not admin/instructor and not owner
- returns message when student is not deleted
- returns null on successful restore
- returns friendly failure message when `SaveChangesAsync` throws

### 5. Instructor, subject, section, and account profile services

**Files**
- `attendance_monitoring/Services/InstructorService.cs`
- `attendance_monitoring/Services/SubjectService.cs`
- `attendance_monitoring/Services/SectionService.cs`
- `attendance_monitoring/Services/Account/ProfileService.cs`

**Why this is high value**
- These are broad CRUD-style services with validation and authorization branches that are not obviously covered by focused service tests.

**Recommended tests**
- email validation and duplicate-handling paths in `InstructorService`
- validation and not-found branches in `SubjectService` create/update/delete methods
- section retrieval and update/delete failure paths in `SectionService`
- transaction and role-aware profile update paths in `ProfileService`

## Highest-Value Integration Test Opportunities

## 1. Full HTTP auth flows with cookies

**Files / endpoints**
- `attendance_monitoring/Program.cs`
- `attendance_monitoring/Controllers/AccountController.cs`
- `AccountController.WebLogin`
- `AccountController.WebRefresh`
- `AccountController.WebLogout`

**Why this is high value**
- These flows depend on middleware, cookies, authentication configuration, and exception handling together.
- Unit tests do not verify actual cookie issuance and request/response behavior.

**Recommended scenarios**
- `POST /api/account/web/login` returns 200 and sets auth cookies for valid credentials
- invalid credentials return 401 and no auth cookies
- `POST /api/account/web/refresh` returns 401 when refresh cookie is missing
- `POST /api/account/web/refresh` rotates cookies when refresh cookie is valid
- `POST /api/account/web/logout` clears cookies and invalidates session state

**Setup notes**
- Best implemented with `WebApplicationFactory` or a reusable `TestServer` host fixture
- Likely needs test auth/test configuration and SQLite-backed EF Core instead of pure mocks

## 2. Admin user-management endpoints

**Files / endpoints**
- `attendance_monitoring/Controllers/UserController.cs`
- `UserController.SoftDeleteUser`
- `UserController.HardDeleteUser`
- `UserController.RestoreUser`

**Why this is high value**
- The controller already has unit coverage, but the real risk is policy enforcement plus service/repository wiring.
- These operations also interact with token revocation and persistence behavior.

**Recommended scenarios**
- admin can soft-delete another user
- admin can restore a previously deleted user
- caller without admin policy receives forbidden
- missing authenticated identity receives unauthorized
- self-delete protection behaves correctly end-to-end

## 3. Fingerprint device endpoints

**Files / endpoints**
- `attendance_monitoring/Controllers/FingerprintController.cs`
- `FingerprintController.GetPendingEnrollmentSession`
- `FingerprintController.CompleteEnrollmentSession`
- `FingerprintController.ScanFingerprintBySensor`

**Why this is high value**
- These endpoints combine header-based device auth, EF persistence, and business rules.
- There are strong service tests in `Services_Testing/FingerprintServiceTest.cs`, but controller and HTTP boundary coverage is still missing.

**Recommended scenarios**
- device request without valid API key returns 401
- pending enrollment returns 204 when no session exists
- valid pending enrollment request returns 200 with payload
- enrollment completion persists result and returns 200
- invalid enrollment payload returns 400
- unknown fingerprint scan returns success false or expected response body without crashing

## 4. QR attendance scan flow

**Files / endpoints**
- `attendance_monitoring/Controllers/QrCodeController.cs`
- `QrCodeController.ScanQrCode`
- related QR services under `attendance_monitoring/Services/QrCode/`

**Why this is high value**
- The endpoint intentionally returns `200 OK` even when business result is unsuccessful, which is easy to regress.
- This is a cross-layer attendance boundary that is better verified through HTTP.

**Recommended scenarios**
- authorized user with valid QR hash gets success response
- invalid or revoked QR code returns `200` with `success: false`
- unauthorized request is blocked by auth middleware
- duplicate scan or invalid session state returns expected business response without 500

## 5. Attendance creation and reporting endpoints

**Files / endpoints**
- `attendance_monitoring/Controllers/AttendanceController.cs`
- `AttendanceController.CreateAttendance`
- `AttendanceController.GetSessionAttendance`
- `AttendanceController.GetAttendanceSummary`

**Why this is high value**
- I found service-level authorization tests in `Services_Testing/AttendanceAuthorizationTests.cs`, but little evidence of HTTP-boundary coverage for attendance endpoints.

**Recommended scenarios**
- creating attendance returns `201 Created` with route metadata when valid
- missing student/session returns `404`
- equivalent duplicate attendance retry returns `200 OK` with the existing record (idempotent behavior)
- authorized student/instructor/admin views get filtered attendance summaries correctly
- unauthorized callers are rejected by the app pipeline

## Medium-Value Quick Wins

These appear to be missing or under-covered at the controller boundary and are relatively cheap to add:

- `attendance_monitoring/Controllers/AttendanceController.cs`
- `attendance_monitoring/Controllers/FingerprintController.cs`
- `attendance_monitoring/Controllers/NotificationPreferenceController.cs`

These are worthwhile, but they rank below auth/token, fingerprint, schedule, and full HTTP integration work.

## Suggested Priority Order

1. `AuthenticationService` + `RefreshTokenService` unit tests
2. `RegistrationService` unit tests
3. `ScheduleService` and `StudentService` unit tests
4. Introduce reusable app-host integration fixture
5. Account web auth integration tests
6. User management integration tests
7. Fingerprint and QR integration tests
8. Missing controller unit tests for remaining controllers

## Infrastructure Recommendation

To unlock the best integration coverage with the least friction:

- add a reusable `WebApplicationFactory`-based test host
- use SQLite for most integration tests to exercise EF and HTTP together
- keep the existing SQL Server-only test pattern for provider-specific behavior such as stored procedures and transaction checks
- continue using environment-gated tests like `RequiresEnvironmentVariableFactAttribute` for SQL Server-only scenarios

## Verified References

- `attendance.testproject/attendance.testproject.csproj`
- `attendance.testproject/Integration_Testing/GlobalExceptionHandlerIntegrationTests.cs`
- `attendance.testproject/Integration_Testing/AccountRepositorySqlServerTransactionIntegrationTests.cs`
- `attendance.testproject/Services_Testing/FingerprintServiceTest.cs`
- `attendance.testproject/Services_Testing/AttendanceAuthorizationTests.cs`
- `attendance.testproject/Services_Testing/SessionServiceTest.cs`
- `attendance_monitoring/Program.cs`
- `attendance_monitoring/Controllers/AccountController.cs`
- `attendance_monitoring/Controllers/UserController.cs`
- `attendance_monitoring/Controllers/FingerprintController.cs`
- `attendance_monitoring/Controllers/QrCodeController.cs`
- `attendance_monitoring/Controllers/AttendanceController.cs`
- `attendance_monitoring/Services/Account/AuthenticationService.cs`
- `attendance_monitoring/Services/Account/RegistrationService.cs`
- `attendance_monitoring/Services/RefreshTokenService.cs`
- `attendance_monitoring/Services/ScheduleService.cs`
- `attendance_monitoring/Services/StudentService.cs`

## Implementation Status (As of April 2026)

### ✅ FULLY IMPLEMENTED

#### Integration Tests
- **AccountAuthIntegrationTests.cs** - Full HTTP auth flows with cookies (login, refresh, logout, cookie handling)
- **QrCodeFlowIntegrationTests.cs** - QR attendance scan flow with comprehensive scenarios
- **AttendanceFlowIntegrationTests.cs** - Attendance creation and reporting endpoints
- **AccountProfileIntegrationTests.cs** - Profile update endpoint with password change
- **PasswordChangeFlowIntegrationTests.cs** - Password change flow with login verification
- **ReportsAuthorizationIntegrationTests.cs** - Policy-based authorization for reports endpoints
- **OperationalReliabilityIntegrationTests.cs** - Correlation ID and metrics instrumentation
- **MiddlewarePipelineIntegrationTests.cs** - Middleware pipeline behavior
- **HealthContractIntegrationTests.cs** - Health check endpoint contracts
- **DatabaseConstraintIntegrationTests.cs** - Database constraint validation
- **GlobalExceptionHandlerIntegrationTests.cs** - Global exception handling
- **AdminUserManagementIntegrationTests.cs** - Admin user-management endpoints (soft/hard delete, restore, policy enforcement)
- **ApiIntegrationHost.cs** - Reusable WebApplicationFactory-based test host (infrastructure recommendation implemented)

#### Unit Tests
- **SectionServiceTest.cs** - Section service with comprehensive coverage including dependency conflict detection
- **FingerprintServiceTest.cs** - Fingerprint service with enrollment session and scan logic
- **FingerprintServiceConcurrencyIntegrationTest.cs** - Fingerprint service concurrency handling
- **ProfileServicePasswordChangeTests.cs** - Profile service password change coverage
- **AuthenticationServiceTest.cs** - Authentication service with login, refresh, logout logic (security-critical)
- **RefreshTokenServiceTest.cs** - Refresh token service with validation, rotation, revocation (security-critical)
- **ScheduleServiceTest.cs** - Schedule service with CreateScheduleAsync/UpdateScheduleAsync validation
- **StudentServiceTest.cs** - Student service with CreateStudentAsync/RestoreStudentAsync authorization and lifecycle
- **StudentEnrollmentServiceTest.cs** - Student enrollment service
- **InstructorServiceTest.cs** - Instructor service with email validation and duplicate handling
- **SubjectServiceTest.cs** - Subject service with validation and not-found branches
- **RegistrationServiceTest.cs** - Comprehensive registration validation including password mismatch, username/email exists, invalid role, section validation, firstname/lastname validation, and user factory handoff
- **SessionServiceTest.cs** - Session service coverage
- **SessionStatusGuardrailTests.cs** - Session status guardrail validation
- **SessionDtoSerializationTest.cs** - Session DTO serialization tests
- **QrCodeScanServiceTest.cs** - QR code scan service
- **QrCodeBoundaryLoggingTests.cs** - QR code boundary logging
- **ReportsServiceTest.cs** - Reports service
- **AttendanceAuthorizationTests.cs** - Attendance authorization logic
- **AttendanceConcurrencyTests.cs** - Attendance concurrency handling
- **AdminDataServiceTests.cs** - Admin data service
- **AdminServiceTransactionTests.cs** - Admin service transaction handling
- **OrphanedUserCleanupServiceTests.cs** - Orphaned user cleanup service
- **DataIntegrityHealthCheckTest.cs** - Data integrity health check
- **JwtConfigurationValidatorTest.cs** - JWT configuration validation
- **ConfiguredTimeZoneProviderTest.cs** - Time zone provider configuration
- **ExceptionHandlingHelperTests.cs** - Exception handling helper utilities
- **ClassroomControllerTest.cs** - Classroom controller with CRUD operation tests and exception handling
- **CourseControllerTest.cs** - Course controller with CRUD operations, user context, and comprehensive error handling
- **SectionControllerTest.cs** - Section controller with expanded coverage including dependency checks, student retrieval, and conflict handling
- **ClassroomControllerDependencyTest.cs** - Classroom controller dependency check endpoints with edge case coverage

### ⚠️ PARTIALLY IMPLEMENTED

#### Unit Tests
- **RoleStandardizationTests.cs** - Legacy teacher role validation (superseded by comprehensive RegistrationServiceTest.cs)
- **UserControllerTest.cs** - Unit tests exist, but missing integration tests for user management endpoints

### ❌ NOT IMPLEMENTED

#### Integration Tests
- **Fingerprint device endpoints** - No FingerprintController integration tests (device auth, enrollment completion, scan by sensor)

### Updated Priority Order

1. ~~Introduce reusable app-host integration fixture~~ ✅ DONE
2. ~~Account web auth integration tests~~ ✅ DONE
3. ~~QR integration tests~~ ✅ DONE
4. ~~Attendance integration tests~~ ✅ DONE
5. ~~AuthenticationService + RefreshTokenService unit tests~~ ✅ DONE (security-critical)
6. ~~ScheduleService unit tests~~ ✅ DONE
7. ~~StudentService unit tests~~ ✅ DONE
8. ~~InstructorService + SubjectService unit tests~~ ✅ DONE
9. ~~RegistrationService unit tests~~ ✅ DONE (comprehensive coverage)
10. ~~Profile service integration tests~~ ✅ DONE (password change flow)
11. ~~Reports authorization integration tests~~ ✅ DONE
12. ~~Operational reliability integration tests~~ ✅ DONE (correlation ID, metrics)
13. ~~Admin user-management integration tests~~ ✅ DONE (unit tests exist, HTTP boundary now covered)
14. **Fingerprint device integration tests** (service tests exist, HTTP boundary missing)
