# Graph Report - .  (2026-04-28)

## Corpus Check
- 507 files · ~0 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 4411 nodes · 5347 edges · 347 communities detected
- Extraction: 77% EXTRACTED · 23% INFERRED · 0% AMBIGUOUS · INFERRED: 1250 edges (avg confidence: 0.5)
- Token cost: 0 input · 0 output

## God Nodes (most connected - your core abstractions)
1. `InstructorServiceTest` - 82 edges
2. `AccountControllerTest` - 60 edges
3. `AdminDataService` - 60 edges
4. `SessionServiceTest` - 53 edges
5. `SectionControllerTest` - 49 edges
6. `AuthenticationServiceTest` - 49 edges
7. `AdminDataServiceTests` - 46 edges
8. `SessionControllerTest` - 45 edges
9. `ScheduleServiceTest` - 45 edges
10. `AccountRepository` - 40 edges

## Surprising Connections (you probably didn't know these)
- `OrphanedUserConstraintTests` --inherits--> `IDisposable`  [EXTRACTED]
  attendance.testproject/Database_Testing/OrphanedUserConstraintTests.cs →   _Bridges community 70 → community 3_
- `DatabaseConstraintIntegrationTests` --inherits--> `IDisposable`  [EXTRACTED]
  attendance.testproject/Integration_Testing/DatabaseConstraintIntegrationTests.cs →   _Bridges community 3 → community 77_
- `ReliabilityTelemetryCollector` --inherits--> `IDisposable`  [EXTRACTED]
  attendance.testproject/Integration_Testing/Support/ApiIntegrationHost.cs →   _Bridges community 3 → community 6_
- `AttendanceRepositoryTest` --inherits--> `IDisposable`  [EXTRACTED]
  attendance.testproject/Repositories_Testing/AttendanceRepositoryTest.cs →   _Bridges community 3 → community 74_
- `FingerprintServiceConcurrencyIntegrationTest` --inherits--> `IDisposable`  [EXTRACTED]
  attendance.testproject/Services_Testing/FingerprintServiceConcurrencyIntegrationTest.cs →   _Bridges community 3 → community 8_

## Communities

### Community 0 - "Community 0"
Cohesion: 0.01
Nodes (121): attendance_monitoring.Migrations, InitialCreateWithIntId, attendance_monitoring.Migrations, UpdatedModels, attendance_monitoring.Migrations, UpdateStudentTableWithUserIdReference, attendance_monitoring.Migrations, UpdateModelsWithValidationChanges (+113 more)

### Community 1 - "Community 1"
Cohesion: 0.01
Nodes (16): AdminDataController, AttendanceController, ClassroomController, ControllerBase, CourseController, FingerprintController, NotificationPreferenceController, QrCodeController (+8 more)

### Community 2 - "Community 2"
Cohesion: 0.02
Nodes (16): AttendanceRepository, CourseRepository, IAttendanceRepository, IAttendanceRepository, IBaseRepository, IBaseRepository, ICourseRepository, ICourseRepository (+8 more)

### Community 3 - "Community 3"
Cohesion: 0.03
Nodes (10): NullScope, AuthenticationServiceTest, IDisposable, InstructorRepositoryTest, QrCodeRepositoryTest, RequestReliabilityTelemetry, SessionRepositoryTest, SliceAUuidRepositoryTest (+2 more)

### Community 4 - "Community 4"
Cohesion: 0.03
Nodes (15): AccountServiceTest, FakeAdminService, FakeAuthenticationService, FakeProfileService, FakeRegistrationService, AdminService, AuthenticationService, IAdminService (+7 more)

### Community 5 - "Community 5"
Cohesion: 0.02
Nodes (1): InstructorServiceTest

### Community 6 - "Community 6"
Cohesion: 0.04
Nodes (14): SqliteTestDatabase, ApiIntegrationHost, ReliabilityTelemetryCollector, TestAuthenticationHandler, AuthenticationHandler, GlobalExceptionHandlerIntegrationTests, TestHostHandle, HealthContractIntegrationTests (+6 more)

### Community 7 - "Community 7"
Cohesion: 0.03
Nodes (6): AccountRepository, attendance_monitoring.Repositories, UserSpResultDto, IAccountRepository, attendance_monitoring.IRepository, IAccountRepository

### Community 8 - "Community 8"
Cohesion: 0.03
Nodes (13): AdminServiceTransactionTests, TestLogger, ThrowingSaveChangesContext, ApplicationDbContext, attendance_monitoring.Data, FingerprintServiceConcurrencyIntegrationTest, FingerprintServiceSqliteTestDbContext, RaceInjectingAttendanceRepository (+5 more)

### Community 9 - "Community 9"
Cohesion: 0.03
Nodes (3): IQrCodeService, IQrCodeService, QrCodeService

### Community 10 - "Community 10"
Cohesion: 0.05
Nodes (15): attendance_monitoring.Exceptions, EntityAlreadyExistsException, attendance_monitoring.Exceptions, EntityConflictException, attendance_monitoring.Exceptions, EntityNotFoundException, attendance_monitoring.Exceptions, EntityServiceException (+7 more)

### Community 11 - "Community 11"
Cohesion: 0.08
Nodes (3): AdminDataService, IAdminDataService, IAdminDataService

### Community 12 - "Community 12"
Cohesion: 0.03
Nodes (1): AccountControllerTest

### Community 13 - "Community 13"
Cohesion: 0.03
Nodes (3): IQrCodeRepository, IQrCodeRepository, QrCodeRepository

### Community 14 - "Community 14"
Cohesion: 0.11
Nodes (1): SessionServiceTest

### Community 15 - "Community 15"
Cohesion: 0.14
Nodes (5): AdminDataServiceTests, TestRetryingExecutionStrategy, TestRetryingExecutionStrategyFactory, ExecutionStrategy, IExecutionStrategyFactory

### Community 16 - "Community 16"
Cohesion: 0.05
Nodes (1): SectionControllerTest

### Community 17 - "Community 17"
Cohesion: 0.05
Nodes (3): IStudentEnrollmentRepository, IStudentEnrollmentRepository, StudentEnrollmentRepository

### Community 18 - "Community 18"
Cohesion: 0.05
Nodes (3): FingerprintRepository, IFingerprintRepository, IFingerprintRepository

### Community 19 - "Community 19"
Cohesion: 0.06
Nodes (1): SessionControllerTest

### Community 20 - "Community 20"
Cohesion: 0.06
Nodes (3): IStudentEnrollmentService, IStudentEnrollmentService, StudentEnrollmentService

### Community 21 - "Community 21"
Cohesion: 0.06
Nodes (7): AttendanceRecordResponseDto, AttendanceRecordResponseDto, AttendanceService, IdempotentAttendanceRetryResponseDto, IAttendanceService, IAttendanceService, IIdempotentAttendanceRetryResult

### Community 22 - "Community 22"
Cohesion: 0.07
Nodes (1): ScheduleServiceTest

### Community 23 - "Community 23"
Cohesion: 0.05
Nodes (3): IInstructorRepository, IInstructorRepository, InstructorRepository

### Community 24 - "Community 24"
Cohesion: 0.06
Nodes (4): IInstructorService, IInstructorService, attendance_monitoring.Services, InstructorService

### Community 25 - "Community 25"
Cohesion: 0.09
Nodes (3): ISessionService, ISessionService, SessionService

### Community 26 - "Community 26"
Cohesion: 0.06
Nodes (5): IScheduleRepository, attendance_monitoring.IRepository, IScheduleRepository, attendance_monitoring.Repositories, ScheduleRepository

### Community 27 - "Community 27"
Cohesion: 0.05
Nodes (1): StudentEnrollmentServiceTest

### Community 28 - "Community 28"
Cohesion: 0.05
Nodes (3): ISessionRepository, ISessionRepository, SessionRepository

### Community 29 - "Community 29"
Cohesion: 0.09
Nodes (1): UserControllerTest

### Community 30 - "Community 30"
Cohesion: 0.13
Nodes (4): DependencyInjectionExtensions, QrCode, QrCodeScanServiceTest, QrCodeServiceTest

### Community 31 - "Community 31"
Cohesion: 0.05
Nodes (3): IStudentRepository, IStudentRepository, StudentRepository

### Community 32 - "Community 32"
Cohesion: 0.06
Nodes (1): SubjectServiceTest

### Community 33 - "Community 33"
Cohesion: 0.06
Nodes (1): StudentEnrollmentControllerTest

### Community 34 - "Community 34"
Cohesion: 0.06
Nodes (5): AccountService, IAccountService, attendance_monitoring.IServices, IAccountService, LoginResult

### Community 35 - "Community 35"
Cohesion: 0.07
Nodes (5): ISectionService, attendance_monitoring.IServices, ISectionService, attendance_monitoring.Services, SectionService

### Community 36 - "Community 36"
Cohesion: 0.07
Nodes (7): AccountRepositoryTransactionTests, TrackingDbCommand, TrackingDbConnection, TrackingDbTransaction, DbCommand, DbConnection, DbTransaction

### Community 37 - "Community 37"
Cohesion: 0.06
Nodes (1): ClassroomServiceTest

### Community 38 - "Community 38"
Cohesion: 0.08
Nodes (5): IScheduleService, attendance_monitoring.IServices, IScheduleService, attendance_monitoring.Services, ScheduleService

### Community 39 - "Community 39"
Cohesion: 0.08
Nodes (12): attendance_monitoring.Migrations, ReissueSessionRowVersionSafely, SnapshotProxy, attendance_monitoring.Migrations, SnapshotProxy, UpdateGetAllUsersSpForProfileUuidCutover, attendance_monitoring.Migrations, SnapshotProxy (+4 more)

### Community 40 - "Community 40"
Cohesion: 0.07
Nodes (1): ScheduleControllerTest

### Community 41 - "Community 41"
Cohesion: 0.06
Nodes (5): ISectionRepository, attendance_monitoring.IRepository, ISectionRepository, attendance_monitoring.Repositories, SectionRepository

### Community 42 - "Community 42"
Cohesion: 0.07
Nodes (1): CourseServiceTest

### Community 43 - "Community 43"
Cohesion: 0.08
Nodes (11): AutomaticSessionEndBackgroundService, BackgroundService, BlacklistedTokenCleanupService, IOrphanedUserCleanupService, IOrphanedUserCleanupService, DataIntegrityStatus, OrphanedUserCleanupService, OrphanedUserMonitoringResult (+3 more)

### Community 44 - "Community 44"
Cohesion: 0.08
Nodes (4): IStudentService, IStudentService, attendance_monitoring.Services, StudentService

### Community 45 - "Community 45"
Cohesion: 0.12
Nodes (1): CourseControllerTest

### Community 46 - "Community 46"
Cohesion: 0.08
Nodes (1): RefreshTokenServiceTest

### Community 47 - "Community 47"
Cohesion: 0.1
Nodes (3): ClassroomService, IClassroomService, IClassroomService

### Community 48 - "Community 48"
Cohesion: 0.1
Nodes (3): ISubjectService, ISubjectService, SubjectService

### Community 49 - "Community 49"
Cohesion: 0.08
Nodes (3): ClassroomRepository, IClassroomRepository, IClassroomRepository

### Community 50 - "Community 50"
Cohesion: 0.08
Nodes (1): InstructorControllerTest

### Community 51 - "Community 51"
Cohesion: 0.12
Nodes (1): QrCodeControllerTest

### Community 52 - "Community 52"
Cohesion: 0.18
Nodes (1): AdminUserManagementIntegrationTests

### Community 53 - "Community 53"
Cohesion: 0.11
Nodes (3): CourseService, ICourseService, ICourseService

### Community 54 - "Community 54"
Cohesion: 0.13
Nodes (3): CookieOptionsService, ICookieOptionsService, ICookieOptionsService

### Community 55 - "Community 55"
Cohesion: 0.11
Nodes (7): attendance_monitoring.Services, DataSeederService, IDataSeederService, attendance_monitoring.IServices, IDataSeederService, CountingSeederService, StartupInitializationIntegrationTests

### Community 56 - "Community 56"
Cohesion: 0.17
Nodes (5): AutomaticSessionEndServiceTest, FixedTimeProvider, ConfiguredTimeZoneProviderTest, FixedTimeProvider, TimeProvider

### Community 57 - "Community 57"
Cohesion: 0.1
Nodes (1): PasswordChangeFlowIntegrationTests

### Community 58 - "Community 58"
Cohesion: 0.12
Nodes (3): INotificationService, INotificationService, NotificationService

### Community 59 - "Community 59"
Cohesion: 0.11
Nodes (1): AttendanceControllerTest

### Community 60 - "Community 60"
Cohesion: 0.11
Nodes (1): StudentControllerTest

### Community 61 - "Community 61"
Cohesion: 0.23
Nodes (1): OperationalReliabilityIntegrationTests

### Community 62 - "Community 62"
Cohesion: 0.25
Nodes (1): AttendanceAuthorizationTests

### Community 63 - "Community 63"
Cohesion: 0.19
Nodes (1): OrphanedUserCleanupServiceTests

### Community 64 - "Community 64"
Cohesion: 0.11
Nodes (1): StudentServiceTest

### Community 65 - "Community 65"
Cohesion: 0.16
Nodes (2): AccountController, attendance_monitoring.Controllers

### Community 66 - "Community 66"
Cohesion: 0.15
Nodes (2): attendance_monitoring.Controllers, SectionController

### Community 67 - "Community 67"
Cohesion: 0.22
Nodes (2): attendance_monitoring.Migrations, PromoteUuidToGuidPrimaryKeys

### Community 68 - "Community 68"
Cohesion: 0.15
Nodes (1): QrCodeWriteService

### Community 69 - "Community 69"
Cohesion: 0.11
Nodes (1): ClassroomControllerTest

### Community 70 - "Community 70"
Cohesion: 0.19
Nodes (1): OrphanedUserConstraintTests

### Community 71 - "Community 71"
Cohesion: 0.11
Nodes (1): ReportsAuthorizationIntegrationTests

### Community 72 - "Community 72"
Cohesion: 0.22
Nodes (1): RegistrationServiceTest

### Community 73 - "Community 73"
Cohesion: 0.17
Nodes (3): IRefreshTokenService, IRefreshTokenService, RefreshTokenService

### Community 74 - "Community 74"
Cohesion: 0.21
Nodes (1): AttendanceRepositoryTest

### Community 75 - "Community 75"
Cohesion: 0.23
Nodes (1): FingerprintServiceTest

### Community 76 - "Community 76"
Cohesion: 0.12
Nodes (1): InstructorController

### Community 77 - "Community 77"
Cohesion: 0.17
Nodes (1): DatabaseConstraintIntegrationTests

### Community 78 - "Community 78"
Cohesion: 0.14
Nodes (3): IReportsService, IReportsService, ReportsService

### Community 79 - "Community 79"
Cohesion: 0.14
Nodes (1): SubjectControllerTest

### Community 80 - "Community 80"
Cohesion: 0.14
Nodes (1): JwtConfigurationValidatorTest

### Community 81 - "Community 81"
Cohesion: 0.14
Nodes (1): SectionServiceTest

### Community 82 - "Community 82"
Cohesion: 0.21
Nodes (3): AutomaticSessionEndService, IAutomaticSessionEndService, IAutomaticSessionEndService

### Community 83 - "Community 83"
Cohesion: 0.14
Nodes (6): LoginDto, RegisterDto, CreateAttendanceRequest, CreateSession, IValidatableObject, UpdateSessionRoom

### Community 84 - "Community 84"
Cohesion: 0.15
Nodes (1): UuidRouteIntegrationTests

### Community 85 - "Community 85"
Cohesion: 0.28
Nodes (4): attendance_monitoring.IServices, IUserFactory, UserCreationResult, UserFactory

### Community 86 - "Community 86"
Cohesion: 0.23
Nodes (1): QrCodeQueryService

### Community 87 - "Community 87"
Cohesion: 0.17
Nodes (1): AccountAuthIntegrationTests

### Community 88 - "Community 88"
Cohesion: 0.33
Nodes (1): AttendanceServiceUuidTests

### Community 89 - "Community 89"
Cohesion: 0.24
Nodes (1): NotificationServiceTest

### Community 90 - "Community 90"
Cohesion: 0.21
Nodes (1): ProfileServicePasswordChangeTests

### Community 91 - "Community 91"
Cohesion: 0.2
Nodes (1): QrCodeFlowIntegrationTests

### Community 92 - "Community 92"
Cohesion: 0.24
Nodes (3): IUserContextService, IUserContextService, UserContextService

### Community 93 - "Community 93"
Cohesion: 0.24
Nodes (4): DatabaseConnectivityHealthCheck, DataIntegrityHealthCheck, DataIntegrityHealthEvaluation, IHealthCheck

### Community 94 - "Community 94"
Cohesion: 0.44
Nodes (1): QrCodeGenerationService

### Community 95 - "Community 95"
Cohesion: 0.42
Nodes (1): QrCodeScanService

### Community 96 - "Community 96"
Cohesion: 0.39
Nodes (1): HighRiskModuleGuardrailTests

### Community 97 - "Community 97"
Cohesion: 0.22
Nodes (1): FingerprintControllerTest

### Community 98 - "Community 98"
Cohesion: 0.22
Nodes (1): AccountProfileIntegrationTests

### Community 99 - "Community 99"
Cohesion: 0.42
Nodes (1): AttendanceFlowIntegrationTests

### Community 100 - "Community 100"
Cohesion: 0.36
Nodes (1): UuidProfileMigrationIntegrationTests

### Community 101 - "Community 101"
Cohesion: 0.31
Nodes (1): MiddlewarePipelineExtensions

### Community 102 - "Community 102"
Cohesion: 0.28
Nodes (1): ScheduleConflictValidator

### Community 103 - "Community 103"
Cohesion: 0.46
Nodes (1): AdminDataControllerTest

### Community 104 - "Community 104"
Cohesion: 0.25
Nodes (1): ClassroomControllerDependencyTest

### Community 105 - "Community 105"
Cohesion: 0.39
Nodes (1): AttendanceConcurrencyTests

### Community 106 - "Community 106"
Cohesion: 0.29
Nodes (1): ExceptionHandlingHelper

### Community 107 - "Community 107"
Cohesion: 0.29
Nodes (2): Hub, NotificationHub

### Community 108 - "Community 108"
Cohesion: 0.25
Nodes (5): IRoleInitializationService, attendance_monitoring.IServices, IRoleInitializationService, attendance_monitoring.Services, RoleInitializationService

### Community 109 - "Community 109"
Cohesion: 0.36
Nodes (2): AddSliceAAcademicUuidColumns, attendance_monitoring.Migrations

### Community 110 - "Community 110"
Cohesion: 0.38
Nodes (1): ServiceArchitectureGuardrailTests

### Community 111 - "Community 111"
Cohesion: 0.48
Nodes (1): InstructorSectionsIntegrationTests

### Community 112 - "Community 112"
Cohesion: 0.29
Nodes (1): ReportsServiceTest

### Community 113 - "Community 113"
Cohesion: 0.29
Nodes (1): SessionStatusGuardrailTests

### Community 114 - "Community 114"
Cohesion: 0.29
Nodes (3): AllowedEmailDomainsAttribute, NotEmptyGuidAttribute, ValidationAttribute

### Community 115 - "Community 115"
Cohesion: 0.33
Nodes (2): InMemoryPreferenceService, INotificationPreferenceService

### Community 116 - "Community 116"
Cohesion: 0.29
Nodes (1): ScheduleServiceSupport

### Community 117 - "Community 117"
Cohesion: 0.47
Nodes (1): ControllerServiceRepositoryGuardrailTests

### Community 118 - "Community 118"
Cohesion: 0.4
Nodes (1): UuidMigrationGuardrailTests

### Community 119 - "Community 119"
Cohesion: 0.53
Nodes (1): TestAuthTokenFactory

### Community 120 - "Community 120"
Cohesion: 0.53
Nodes (1): DataIntegrityHealthCheckTest

### Community 121 - "Community 121"
Cohesion: 0.33
Nodes (1): RoleStandardizationTests

### Community 122 - "Community 122"
Cohesion: 0.33
Nodes (3): ITokenValidationService, ITokenValidationService, TokenValidationService

### Community 123 - "Community 123"
Cohesion: 0.33
Nodes (1): JwtConfigurationValidator

### Community 124 - "Community 124"
Cohesion: 0.33
Nodes (0): 

### Community 125 - "Community 125"
Cohesion: 0.4
Nodes (1): CourseControllerDependencyTest

### Community 126 - "Community 126"
Cohesion: 0.7
Nodes (1): AdminUserManagementSeedData

### Community 127 - "Community 127"
Cohesion: 0.5
Nodes (2): AttendanceQrScenarioContext, AttendanceQrSeedData

### Community 128 - "Community 128"
Cohesion: 0.5
Nodes (2): ReportsScenarioContext, ReportsSeedData

### Community 129 - "Community 129"
Cohesion: 0.6
Nodes (1): AutomaticSessionEndBackgroundServiceTest

### Community 130 - "Community 130"
Cohesion: 0.4
Nodes (1): EntityIdResolutionHelperTest

### Community 131 - "Community 131"
Cohesion: 0.5
Nodes (1): AuthenticationServiceExtensions

### Community 132 - "Community 132"
Cohesion: 0.4
Nodes (1): SignalRServiceExtensions

### Community 133 - "Community 133"
Cohesion: 0.4
Nodes (4): AdminProfileDto, GetAllUsersDto, InstructorProfileDto, StudentProfileDto

### Community 134 - "Community 134"
Cohesion: 0.4
Nodes (4): InstructorStudentAttendanceSummaryDto, InstructorStudentDetailDto, InstructorStudentEnrollmentDto, InstructorStudentFingerprintDto

### Community 135 - "Community 135"
Cohesion: 0.4
Nodes (4): AdminProfileInfo, InstructorProfileInfo, StudentProfileInfo, UserProfileResponseDto

### Community 136 - "Community 136"
Cohesion: 0.4
Nodes (1): QrCodeAuthorizationService

### Community 137 - "Community 137"
Cohesion: 0.4
Nodes (0): 

### Community 138 - "Community 138"
Cohesion: 0.67
Nodes (1): RoleAuthorizationGuardrailTests

### Community 139 - "Community 139"
Cohesion: 0.67
Nodes (1): InstructorSeedData

### Community 140 - "Community 140"
Cohesion: 0.5
Nodes (1): RequestGuidValidationTests

### Community 141 - "Community 141"
Cohesion: 0.5
Nodes (1): ExceptionHandlingHelperTests

### Community 142 - "Community 142"
Cohesion: 0.5
Nodes (1): SessionDtoSerializationTest

### Community 143 - "Community 143"
Cohesion: 0.5
Nodes (1): EnrollmentTypeConstants

### Community 144 - "Community 144"
Cohesion: 0.67
Nodes (1): RoleConstants

### Community 145 - "Community 145"
Cohesion: 0.5
Nodes (1): SessionStatusConstants

### Community 146 - "Community 146"
Cohesion: 0.5
Nodes (2): ApplicationDbContextFactory, IDesignTimeDbContextFactory

### Community 147 - "Community 147"
Cohesion: 0.67
Nodes (1): ConfiguredTimeZoneProvider

### Community 148 - "Community 148"
Cohesion: 0.5
Nodes (1): ResponseHandlingExtensions

### Community 149 - "Community 149"
Cohesion: 0.67
Nodes (1): EntityIdResolutionHelper

### Community 150 - "Community 150"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, InitialCreateWithIntId

### Community 151 - "Community 151"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, UpdatedModels

### Community 152 - "Community 152"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, UpdateStudentTableWithUserIdReference

### Community 153 - "Community 153"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, UpdateModelsWithValidationChanges

### Community 154 - "Community 154"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, InstructorModelsAddition

### Community 155 - "Community 155"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, RefreshTokens

### Community 156 - "Community 156"
Cohesion: 0.5
Nodes (2): AddSectionNavigationToCourseAndInstructor, attendance_monitoring.Migrations

### Community 157 - "Community 157"
Cohesion: 0.5
Nodes (2): AddAdminClassroomCourseScheduleSubject, attendance_monitoring.Migrations

### Community 158 - "Community 158"
Cohesion: 0.5
Nodes (2): AddUniqueConstraintsForEntities, attendance_monitoring.Migrations

### Community 159 - "Community 159"
Cohesion: 0.5
Nodes (2): AddSoftDeleteToInstructorAndStudent, attendance_monitoring.Migrations

### Community 160 - "Community 160"
Cohesion: 0.5
Nodes (2): AddBlacklistedTokensTable, attendance_monitoring.Migrations

### Community 161 - "Community 161"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, TokenIndexes

### Community 162 - "Community 162"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, StudentIsDeletedIndex

### Community 163 - "Community 163"
Cohesion: 0.5
Nodes (2): AddCompositeIndexOnRefreshTokens, attendance_monitoring.Migrations

### Community 164 - "Community 164"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, UpdateScheduleEntity

### Community 165 - "Community 165"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, IndexAndCompositeKey

### Community 166 - "Community 166"
Cohesion: 0.5
Nodes (2): AddInstructorIdToSchedule, attendance_monitoring.Migrations

### Community 167 - "Community 167"
Cohesion: 0.5
Nodes (2): AddIsRegularToStudent, attendance_monitoring.Migrations

### Community 168 - "Community 168"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, RemoveInstructorIdFromSection

### Community 169 - "Community 169"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, FixSectionInstructorRelationship

### Community 170 - "Community 170"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, CreateQrCodesTable

### Community 171 - "Community 171"
Cohesion: 0.5
Nodes (2): AddQrCodeRevocationAuditTrail, attendance_monitoring.Migrations

### Community 172 - "Community 172"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, PendingChanges

### Community 173 - "Community 173"
Cohesion: 0.5
Nodes (2): AddStudentEnrollmentSystem, attendance_monitoring.Migrations

### Community 174 - "Community 174"
Cohesion: 0.5
Nodes (2): AddSessionAndAttendanceEntities, attendance_monitoring.Migrations

### Community 175 - "Community 175"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, RemoveRedundantEmailFields

### Community 176 - "Community 176"
Cohesion: 0.5
Nodes (2): AddUniqueEmailConstraint, attendance_monitoring.Migrations

### Community 177 - "Community 177"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, MakeStudentNamesRequired

### Community 178 - "Community 178"
Cohesion: 0.5
Nodes (2): AddGetAllUsersSP, attendance_monitoring.Migrations

### Community 179 - "Community 179"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, FixGetAllUsersSP

### Community 180 - "Community 180"
Cohesion: 0.5
Nodes (2): AddSoftDeleteToAdmin, attendance_monitoring.Migrations

### Community 181 - "Community 181"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, CheckPendingChanges

### Community 182 - "Community 182"
Cohesion: 0.5
Nodes (2): AddDeleteUserSP, attendance_monitoring.Migrations

### Community 183 - "Community 183"
Cohesion: 0.5
Nodes (2): AddHardDeleteUserSP, attendance_monitoring.Migrations

### Community 184 - "Community 184"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, FixHardDeleteUserSPSignature

### Community 185 - "Community 185"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, FixHardDeleteUserSPColumnName

### Community 186 - "Community 186"
Cohesion: 0.5
Nodes (2): AddUserStatusFilterToGetAllUsersSP, attendance_monitoring.Migrations

### Community 187 - "Community 187"
Cohesion: 0.5
Nodes (2): AddRestoreUserSP, attendance_monitoring.Migrations

### Community 188 - "Community 188"
Cohesion: 0.5
Nodes (2): AddOrphanedUserPreventionConstraints, attendance_monitoring.Migrations

### Community 189 - "Community 189"
Cohesion: 0.5
Nodes (2): AddQrCodeRowVersion, attendance_monitoring.Migrations

### Community 190 - "Community 190"
Cohesion: 0.5
Nodes (2): AddFingerprintDeviceAndScanEvents, attendance_monitoring.Migrations

### Community 191 - "Community 191"
Cohesion: 0.5
Nodes (2): AddFingerprintEnrollmentSessionsAndDeviceFlow, attendance_monitoring.Migrations

### Community 192 - "Community 192"
Cohesion: 0.5
Nodes (2): AddSessionRowVersionConcurrency, attendance_monitoring.Migrations

### Community 193 - "Community 193"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, MakeSessionRowVersionNonNullable

### Community 194 - "Community 194"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, MakeSessionRowVersionNonNullableAndFixStoredProcedure

### Community 195 - "Community 195"
Cohesion: 0.5
Nodes (2): AddInstructorDepartmentAndSpecialization, attendance_monitoring.Migrations

### Community 196 - "Community 196"
Cohesion: 0.5
Nodes (2): AddWave1ProfileUuidColumns, attendance_monitoring.Migrations

### Community 197 - "Community 197"
Cohesion: 0.5
Nodes (2): AddSliceAAcademicUuidColumns, attendance_monitoring.Migrations

### Community 198 - "Community 198"
Cohesion: 0.5
Nodes (2): AddSliceBAttendanceUuidColumns, attendance_monitoring.Migrations

### Community 199 - "Community 199"
Cohesion: 0.5
Nodes (2): AddFingerprintSupportUuidColumns, attendance_monitoring.Migrations

### Community 200 - "Community 200"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, PromoteUuidToGuidPrimaryKeys

### Community 201 - "Community 201"
Cohesion: 0.5
Nodes (2): AddUsnToStudents, attendance_monitoring.Migrations

### Community 202 - "Community 202"
Cohesion: 0.5
Nodes (2): AddUsnUniqueIndex, attendance_monitoring.Migrations

### Community 203 - "Community 203"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, BackfillUsnForExistingStudents

### Community 204 - "Community 204"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, MakeUsnRequired

### Community 205 - "Community 205"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, FixScheduleUniqueConstraint

### Community 206 - "Community 206"
Cohesion: 0.5
Nodes (2): attendance_monitoring.Migrations, RemoveLegacyIntIdColumns

### Community 207 - "Community 207"
Cohesion: 0.5
Nodes (3): InstructorSessionItemDto, InstructorSessionsReportDto, SessionAttendanceStatsDto

### Community 208 - "Community 208"
Cohesion: 0.5
Nodes (3): EnrollmentSummaryDto, StudentEnrollmentResponseDto, StudentSectionsResponseDto

### Community 209 - "Community 209"
Cohesion: 0.67
Nodes (1): DependencyRouteContractTest

### Community 210 - "Community 210"
Cohesion: 0.67
Nodes (1): ReportsControllerTest

### Community 211 - "Community 211"
Cohesion: 0.67
Nodes (1): AccountSeedData

### Community 212 - "Community 212"
Cohesion: 0.67
Nodes (2): FactAttribute, RequiresEnvironmentVariableFactAttribute

### Community 213 - "Community 213"
Cohesion: 0.67
Nodes (1): Student

### Community 214 - "Community 214"
Cohesion: 0.67
Nodes (1): ScheduleConstants

### Community 215 - "Community 215"
Cohesion: 0.67
Nodes (1): ApiDocumentationExtensions

### Community 216 - "Community 216"
Cohesion: 0.67
Nodes (1): DatabaseServiceExtensions

### Community 217 - "Community 217"
Cohesion: 0.67
Nodes (1): LoggingServiceExtensions

### Community 218 - "Community 218"
Cohesion: 0.67
Nodes (1): ExceptionHandlingExtensions

### Community 219 - "Community 219"
Cohesion: 0.67
Nodes (1): HealthCheckExtensions

### Community 220 - "Community 220"
Cohesion: 0.67
Nodes (1): StartupExtensions

### Community 221 - "Community 221"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Request, CreateSchedule

### Community 222 - "Community 222"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Request, CreateSection

### Community 223 - "Community 223"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Request, UpdateSchedule

### Community 224 - "Community 224"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Response, CheckAuthResponseDto

### Community 225 - "Community 225"
Cohesion: 0.67
Nodes (2): ClassAttendanceSummaryDto, SessionAttendanceStatsDto

### Community 226 - "Community 226"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Response, ClassroomResponseDto

### Community 227 - "Community 227"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Response, DeleteUserResponseDto

### Community 228 - "Community 228"
Cohesion: 0.67
Nodes (2): InstructorHandledClassDto, InstructorHandledClassStudentDto

### Community 229 - "Community 229"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Response, InstructorResponseDto

### Community 230 - "Community 230"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Response, RefreshResponseDto

### Community 231 - "Community 231"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Response, RegisterResponseDto

### Community 232 - "Community 232"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Response, RevokeResponseDto

### Community 233 - "Community 233"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Response, ScheduleResponseDto

### Community 234 - "Community 234"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Response, SectionResponseDto

### Community 235 - "Community 235"
Cohesion: 0.67
Nodes (2): SessionAttendanceDto, StudentAttendanceRecordDto

### Community 236 - "Community 236"
Cohesion: 0.67
Nodes (2): StudentSubjectResponseDto, StudentSubjectScheduleDto

### Community 237 - "Community 237"
Cohesion: 0.67
Nodes (2): attendance_monitoring.Models.DTO.Response, SubjectResponseDto

### Community 238 - "Community 238"
Cohesion: 0.67
Nodes (1): QrCodeMapper

### Community 239 - "Community 239"
Cohesion: 1.0
Nodes (1): AccountScenarioContext

### Community 240 - "Community 240"
Cohesion: 1.0
Nodes (1): AdminUserManagementScenarioContext

### Community 241 - "Community 241"
Cohesion: 1.0
Nodes (1): InstructorScenarioContext

### Community 242 - "Community 242"
Cohesion: 1.0
Nodes (1): Admin

### Community 243 - "Community 243"
Cohesion: 1.0
Nodes (1): AttendanceRecord

### Community 244 - "Community 244"
Cohesion: 1.0
Nodes (1): BlacklistedToken

### Community 245 - "Community 245"
Cohesion: 1.0
Nodes (1): Classroom

### Community 246 - "Community 246"
Cohesion: 1.0
Nodes (1): Course

### Community 247 - "Community 247"
Cohesion: 1.0
Nodes (1): Fingerprint

### Community 248 - "Community 248"
Cohesion: 1.0
Nodes (1): FingerprintDevice

### Community 249 - "Community 249"
Cohesion: 1.0
Nodes (1): FingerprintEnrollmentSession

### Community 250 - "Community 250"
Cohesion: 1.0
Nodes (1): FingerprintScanEvent

### Community 251 - "Community 251"
Cohesion: 1.0
Nodes (1): Instructor

### Community 252 - "Community 252"
Cohesion: 1.0
Nodes (1): RefreshToken

### Community 253 - "Community 253"
Cohesion: 1.0
Nodes (1): Schedules

### Community 254 - "Community 254"
Cohesion: 1.0
Nodes (1): ScheduleConflictDetails

### Community 255 - "Community 255"
Cohesion: 1.0
Nodes (1): Section

### Community 256 - "Community 256"
Cohesion: 1.0
Nodes (1): Session

### Community 257 - "Community 257"
Cohesion: 1.0
Nodes (1): StudentEnrollment

### Community 258 - "Community 258"
Cohesion: 1.0
Nodes (1): Subject

### Community 259 - "Community 259"
Cohesion: 1.0
Nodes (1): PaginationConstants

### Community 260 - "Community 260"
Cohesion: 1.0
Nodes (1): TokenConstants

### Community 261 - "Community 261"
Cohesion: 1.0
Nodes (1): RefreshTokenRequestDto

### Community 262 - "Community 262"
Cohesion: 1.0
Nodes (1): AdminUpdateUser

### Community 263 - "Community 263"
Cohesion: 1.0
Nodes (1): AttendanceFilterRequest

### Community 264 - "Community 264"
Cohesion: 1.0
Nodes (1): CancelSession

### Community 265 - "Community 265"
Cohesion: 1.0
Nodes (1): CompleteFingerprintEnrollmentRequest

### Community 266 - "Community 266"
Cohesion: 1.0
Nodes (1): CreateClassroom

### Community 267 - "Community 267"
Cohesion: 1.0
Nodes (1): CreateCourse

### Community 268 - "Community 268"
Cohesion: 1.0
Nodes (1): CreateInstructor

### Community 269 - "Community 269"
Cohesion: 1.0
Nodes (1): CreateQrCode

### Community 270 - "Community 270"
Cohesion: 1.0
Nodes (1): CreateStudent

### Community 271 - "Community 271"
Cohesion: 1.0
Nodes (1): CreateStudentEnrollment

### Community 272 - "Community 272"
Cohesion: 1.0
Nodes (1): CreateSubject

### Community 273 - "Community 273"
Cohesion: 1.0
Nodes (1): EndSession

### Community 274 - "Community 274"
Cohesion: 1.0
Nodes (1): FileUploadRequest

### Community 275 - "Community 275"
Cohesion: 1.0
Nodes (1): PaginationQuery

### Community 276 - "Community 276"
Cohesion: 1.0
Nodes (1): QrCodeRequest

### Community 277 - "Community 277"
Cohesion: 1.0
Nodes (1): RevokeQrCode

### Community 278 - "Community 278"
Cohesion: 1.0
Nodes (1): ScanFingerprintBySensorRequest

### Community 279 - "Community 279"
Cohesion: 1.0
Nodes (1): StartFingerprintEnrollmentSessionRequest

### Community 280 - "Community 280"
Cohesion: 1.0
Nodes (1): StartSession

### Community 281 - "Community 281"
Cohesion: 1.0
Nodes (1): UpdateAttendanceRequest

### Community 282 - "Community 282"
Cohesion: 1.0
Nodes (1): UpdateClassroom

### Community 283 - "Community 283"
Cohesion: 1.0
Nodes (1): UpdateCourse

### Community 284 - "Community 284"
Cohesion: 1.0
Nodes (1): UpdateInstructor

### Community 285 - "Community 285"
Cohesion: 1.0
Nodes (1): UpdateProfile

### Community 286 - "Community 286"
Cohesion: 1.0
Nodes (1): UpdateQrCode

### Community 287 - "Community 287"
Cohesion: 1.0
Nodes (1): UpdateStudent

### Community 288 - "Community 288"
Cohesion: 1.0
Nodes (1): UpdateSubject

### Community 289 - "Community 289"
Cohesion: 1.0
Nodes (1): attendance_monitoring.Models.DTO.Request

### Community 290 - "Community 290"
Cohesion: 1.0
Nodes (1): ValidateQrCode

### Community 291 - "Community 291"
Cohesion: 1.0
Nodes (1): AdminDataFileDto

### Community 292 - "Community 292"
Cohesion: 1.0
Nodes (1): AdminDataImportResponseDto

### Community 293 - "Community 293"
Cohesion: 1.0
Nodes (1): AdminDataIssueDto

### Community 294 - "Community 294"
Cohesion: 1.0
Nodes (1): AdminDataPreviewResponseDto

### Community 295 - "Community 295"
Cohesion: 1.0
Nodes (1): AdminDataRowResultDto

### Community 296 - "Community 296"
Cohesion: 1.0
Nodes (1): AttendanceListDto

### Community 297 - "Community 297"
Cohesion: 1.0
Nodes (1): AttendanceMinimalDto

### Community 298 - "Community 298"
Cohesion: 1.0
Nodes (1): AttendanceSummaryDto

### Community 299 - "Community 299"
Cohesion: 1.0
Nodes (1): ErrorResponseDto

### Community 300 - "Community 300"
Cohesion: 1.0
Nodes (1): FingerprintDeviceResponseDto

### Community 301 - "Community 301"
Cohesion: 1.0
Nodes (1): FingerprintEnrollmentSessionResponseDto

### Community 302 - "Community 302"
Cohesion: 1.0
Nodes (1): FingerprintRegistrationResponseDto

### Community 303 - "Community 303"
Cohesion: 1.0
Nodes (1): FingerprintResponseDto

### Community 304 - "Community 304"
Cohesion: 1.0
Nodes (1): FingerprintScanResponseDto

### Community 305 - "Community 305"
Cohesion: 1.0
Nodes (1): InstructorHomeSectionStudentDto

### Community 306 - "Community 306"
Cohesion: 1.0
Nodes (1): InstructorProfileResponseDto

### Community 307 - "Community 307"
Cohesion: 1.0
Nodes (1): InstructorSectionDetailDto

### Community 308 - "Community 308"
Cohesion: 1.0
Nodes (1): InstructorSectionOverviewDto

### Community 309 - "Community 309"
Cohesion: 1.0
Nodes (1): InstructorSectionsWithStudentsResponseDto

### Community 310 - "Community 310"
Cohesion: 1.0
Nodes (1): LoginResponseDto

### Community 311 - "Community 311"
Cohesion: 1.0
Nodes (1): LogoutResponseDto

### Community 312 - "Community 312"
Cohesion: 1.0
Nodes (1): PagedResult

### Community 313 - "Community 313"
Cohesion: 1.0
Nodes (1): QrCodeGenerationResponseDto

### Community 314 - "Community 314"
Cohesion: 1.0
Nodes (1): QrCodeInfoDto

### Community 315 - "Community 315"
Cohesion: 1.0
Nodes (1): QrCodeResponseDto

### Community 316 - "Community 316"
Cohesion: 1.0
Nodes (1): QrCodeScanHistoryItemDto

### Community 317 - "Community 317"
Cohesion: 1.0
Nodes (1): QrCodeScanHistoryResponseDto

### Community 318 - "Community 318"
Cohesion: 1.0
Nodes (1): QrCodeScanResponseDto

### Community 319 - "Community 319"
Cohesion: 1.0
Nodes (1): QrCodeStatisticsDto

### Community 320 - "Community 320"
Cohesion: 1.0
Nodes (1): QrCodeValidationResponseDto

### Community 321 - "Community 321"
Cohesion: 1.0
Nodes (1): SectionWithStudentsDto

### Community 322 - "Community 322"
Cohesion: 1.0
Nodes (1): SessionAttendanceRosterDto

### Community 323 - "Community 323"
Cohesion: 1.0
Nodes (1): SessionReportRowDto

### Community 324 - "Community 324"
Cohesion: 1.0
Nodes (1): SessionResponseDto

### Community 325 - "Community 325"
Cohesion: 1.0
Nodes (1): SoftDeleteResponse

### Community 326 - "Community 326"
Cohesion: 1.0
Nodes (1): StudentAttendanceHistoryDto

### Community 327 - "Community 327"
Cohesion: 1.0
Nodes (1): StudentDto

### Community 328 - "Community 328"
Cohesion: 1.0
Nodes (1): StudentListDto

### Community 329 - "Community 329"
Cohesion: 1.0
Nodes (1): SubjectScheduleDto

### Community 330 - "Community 330"
Cohesion: 1.0
Nodes (1): UpdateProfileResponse

### Community 331 - "Community 331"
Cohesion: 1.0
Nodes (1): WebLoginResponseDto

### Community 332 - "Community 332"
Cohesion: 1.0
Nodes (1): WebRefreshResponseDto

### Community 333 - "Community 333"
Cohesion: 1.0
Nodes (1): WebRevokeResponseDto

### Community 334 - "Community 334"
Cohesion: 1.0
Nodes (1): RevokeTokenRequestDto

### Community 335 - "Community 335"
Cohesion: 1.0
Nodes (1): TokenResponseDto

### Community 336 - "Community 336"
Cohesion: 1.0
Nodes (1): WebLoginDto

### Community 337 - "Community 337"
Cohesion: 1.0
Nodes (1): BulkDataOptions

### Community 338 - "Community 338"
Cohesion: 1.0
Nodes (1): SessionAutoEndOptions

### Community 339 - "Community 339"
Cohesion: 1.0
Nodes (1): TimeZoneSettings

### Community 340 - "Community 340"
Cohesion: 1.0
Nodes (0): 

### Community 341 - "Community 341"
Cohesion: 1.0
Nodes (0): 

### Community 342 - "Community 342"
Cohesion: 1.0
Nodes (0): 

### Community 343 - "Community 343"
Cohesion: 1.0
Nodes (0): 

### Community 344 - "Community 344"
Cohesion: 1.0
Nodes (0): 

### Community 345 - "Community 345"
Cohesion: 1.0
Nodes (0): 

### Community 346 - "Community 346"
Cohesion: 1.0
Nodes (0): 

## Knowledge Gaps
- **314 isolated node(s):** `AccountScenarioContext`, `AdminUserManagementScenarioContext`, `AttendanceQrScenarioContext`, `InstructorScenarioContext`, `ReportsScenarioContext` (+309 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **Thin community `Community 239`** (2 nodes): `AccountScenarioContext.cs`, `AccountScenarioContext`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 240`** (2 nodes): `AdminUserManagementScenarioContext.cs`, `AdminUserManagementScenarioContext`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 241`** (2 nodes): `InstructorScenarioContext.cs`, `InstructorScenarioContext`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 242`** (2 nodes): `Admin.cs`, `Admin`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 243`** (2 nodes): `AttendanceRecord.cs`, `AttendanceRecord`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 244`** (2 nodes): `BlacklistedToken.cs`, `BlacklistedToken`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 245`** (2 nodes): `Classroom.cs`, `Classroom`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 246`** (2 nodes): `Course.cs`, `Course`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 247`** (2 nodes): `Fingerprint.cs`, `Fingerprint`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 248`** (2 nodes): `FingerprintDevice.cs`, `FingerprintDevice`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 249`** (2 nodes): `FingerprintEnrollmentSession.cs`, `FingerprintEnrollmentSession`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 250`** (2 nodes): `FingerprintScanEvent.cs`, `FingerprintScanEvent`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 251`** (2 nodes): `Instructor.cs`, `Instructor`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 252`** (2 nodes): `RefreshToken.cs`, `RefreshToken`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 253`** (2 nodes): `Schedule.cs`, `Schedules`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 254`** (2 nodes): `ScheduleConflictDetails.cs`, `ScheduleConflictDetails`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 255`** (2 nodes): `Section.cs`, `Section`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 256`** (2 nodes): `Session.cs`, `Session`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 257`** (2 nodes): `StudentEnrollment.cs`, `StudentEnrollment`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 258`** (2 nodes): `Subject.cs`, `Subject`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 259`** (2 nodes): `PaginationConstants.cs`, `PaginationConstants`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 260`** (2 nodes): `TokenConstants.cs`, `TokenConstants`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 261`** (2 nodes): `RefreshTokenRequestDto.cs`, `RefreshTokenRequestDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 262`** (2 nodes): `AdminUpdateUser.cs`, `AdminUpdateUser`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 263`** (2 nodes): `AttendanceFilterRequest.cs`, `AttendanceFilterRequest`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 264`** (2 nodes): `CancelSession.cs`, `CancelSession`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 265`** (2 nodes): `CompleteFingerprintEnrollmentRequest.cs`, `CompleteFingerprintEnrollmentRequest`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 266`** (2 nodes): `CreateClassroom.cs`, `CreateClassroom`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 267`** (2 nodes): `CreateCourse.cs`, `CreateCourse`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 268`** (2 nodes): `CreateInstructor.cs`, `CreateInstructor`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 269`** (2 nodes): `CreateQrCode.cs`, `CreateQrCode`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 270`** (2 nodes): `CreateStudent.cs`, `CreateStudent`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 271`** (2 nodes): `CreateStudentEnrollment.cs`, `CreateStudentEnrollment`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 272`** (2 nodes): `CreateSubject.cs`, `CreateSubject`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 273`** (2 nodes): `EndSession.cs`, `EndSession`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 274`** (2 nodes): `FileUploadRequest.cs`, `FileUploadRequest`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 275`** (2 nodes): `PaginationQuery.cs`, `PaginationQuery`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 276`** (2 nodes): `QrCodeRequest.cs`, `QrCodeRequest`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 277`** (2 nodes): `RevokeQrCode.cs`, `RevokeQrCode`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 278`** (2 nodes): `ScanFingerprintBySensorRequest.cs`, `ScanFingerprintBySensorRequest`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 279`** (2 nodes): `StartFingerprintEnrollmentSessionRequest.cs`, `StartFingerprintEnrollmentSessionRequest`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 280`** (2 nodes): `StartSession.cs`, `StartSession`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 281`** (2 nodes): `UpdateAttendanceRequest.cs`, `UpdateAttendanceRequest`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 282`** (2 nodes): `UpdateClassroom.cs`, `UpdateClassroom`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 283`** (2 nodes): `UpdateCourse.cs`, `UpdateCourse`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 284`** (2 nodes): `UpdateInstructor.cs`, `UpdateInstructor`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 285`** (2 nodes): `UpdateProfile.cs`, `UpdateProfile`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 286`** (2 nodes): `UpdateQrCode.cs`, `UpdateQrCode`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 287`** (2 nodes): `UpdateStudent.cs`, `UpdateStudent`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 288`** (2 nodes): `UpdateSubject.cs`, `UpdateSubject`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 289`** (2 nodes): `UserStatusFilter.cs`, `attendance_monitoring.Models.DTO.Request`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 290`** (2 nodes): `ValidateQrCode.cs`, `ValidateQrCode`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 291`** (2 nodes): `AdminDataFileDto.cs`, `AdminDataFileDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 292`** (2 nodes): `AdminDataImportResponseDto.cs`, `AdminDataImportResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 293`** (2 nodes): `AdminDataIssueDto.cs`, `AdminDataIssueDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 294`** (2 nodes): `AdminDataPreviewResponseDto.cs`, `AdminDataPreviewResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 295`** (2 nodes): `AdminDataRowResultDto.cs`, `AdminDataRowResultDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 296`** (2 nodes): `AttendanceListDto.cs`, `AttendanceListDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 297`** (2 nodes): `AttendanceMinimalDto.cs`, `AttendanceMinimalDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 298`** (2 nodes): `AttendanceSummaryDto.cs`, `AttendanceSummaryDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 299`** (2 nodes): `ErrorResponseDto.cs`, `ErrorResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 300`** (2 nodes): `FingerprintDeviceResponseDto.cs`, `FingerprintDeviceResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 301`** (2 nodes): `FingerprintEnrollmentSessionResponseDto.cs`, `FingerprintEnrollmentSessionResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 302`** (2 nodes): `FingerprintRegistrationResponseDto.cs`, `FingerprintRegistrationResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 303`** (2 nodes): `FingerprintResponseDto.cs`, `FingerprintResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 304`** (2 nodes): `FingerprintScanResponseDto.cs`, `FingerprintScanResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 305`** (2 nodes): `InstructorHomeSectionStudentDto.cs`, `InstructorHomeSectionStudentDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 306`** (2 nodes): `InstructorProfileResponseDto.cs`, `InstructorProfileResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 307`** (2 nodes): `InstructorSectionDetailDto.cs`, `InstructorSectionDetailDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 308`** (2 nodes): `InstructorSectionOverviewDto.cs`, `InstructorSectionOverviewDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 309`** (2 nodes): `InstructorSectionsWithStudentsResponseDto.cs`, `InstructorSectionsWithStudentsResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 310`** (2 nodes): `LoginResponseDto.cs`, `LoginResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 311`** (2 nodes): `LogoutResponseDto.cs`, `LogoutResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 312`** (2 nodes): `PagedResult.cs`, `PagedResult`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 313`** (2 nodes): `QrCodeGenerationResponseDto.cs`, `QrCodeGenerationResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 314`** (2 nodes): `QrCodeInfoDto.cs`, `QrCodeInfoDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 315`** (2 nodes): `QrCodeResponseDto.cs`, `QrCodeResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 316`** (2 nodes): `QrCodeScanHistoryItemDto.cs`, `QrCodeScanHistoryItemDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 317`** (2 nodes): `QrCodeScanHistoryResponseDto.cs`, `QrCodeScanHistoryResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 318`** (2 nodes): `QrCodeScanResponseDto.cs`, `QrCodeScanResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 319`** (2 nodes): `QrCodeStatisticsDto.cs`, `QrCodeStatisticsDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 320`** (2 nodes): `QrCodeValidationResponseDto.cs`, `QrCodeValidationResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 321`** (2 nodes): `SectionWithStudentsDto.cs`, `SectionWithStudentsDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 322`** (2 nodes): `SessionAttendanceRosterDto.cs`, `SessionAttendanceRosterDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 323`** (2 nodes): `SessionReportRowDto.cs`, `SessionReportRowDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 324`** (2 nodes): `SessionResponseDto.cs`, `SessionResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 325`** (2 nodes): `SoftDeleteResponse.cs`, `SoftDeleteResponse`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 326`** (2 nodes): `StudentAttendanceHistoryDto.cs`, `StudentAttendanceHistoryDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 327`** (2 nodes): `StudentDto.cs`, `StudentDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 328`** (2 nodes): `StudentListDto.cs`, `StudentListDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 329`** (2 nodes): `SubjectScheduleDto.cs`, `SubjectScheduleDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 330`** (2 nodes): `UpdateProfileResponse.cs`, `UpdateProfileResponse`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 331`** (2 nodes): `WebLoginResponseDto.cs`, `WebLoginResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 332`** (2 nodes): `WebRefreshResponseDto.cs`, `WebRefreshResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 333`** (2 nodes): `WebRevokeResponseDto.cs`, `WebRevokeResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 334`** (2 nodes): `RevokeTokenRequestDto.cs`, `RevokeTokenRequestDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 335`** (2 nodes): `TokenResponseDto.cs`, `TokenResponseDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 336`** (2 nodes): `WebLoginDto.cs`, `WebLoginDto`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 337`** (2 nodes): `BulkDataOptions.cs`, `BulkDataOptions`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 338`** (2 nodes): `SessionAutoEndOptions.cs`, `SessionAutoEndOptions`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 339`** (2 nodes): `TimeZoneSettings.cs`, `TimeZoneSettings`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 340`** (1 nodes): `attendance.testproject.AssemblyInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 341`** (1 nodes): `attendance.testproject.GlobalUsings.g.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 342`** (1 nodes): `NotificationDto.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 343`** (1 nodes): `Program.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 344`** (1 nodes): `attendance_monitoring.AssemblyInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 345`** (1 nodes): `attendance_monitoring.GlobalUsings.g.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 346`** (1 nodes): `attendance_monitoring.MvcApplicationPartsAssemblyInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `RaceInjectingAttendanceRepository` connect `Community 8` to `Community 2`?**
  _High betweenness centrality (0.031) - this node is a cross-community bridge._
- **Why does `FingerprintServiceConcurrencyIntegrationTest` connect `Community 8` to `Community 3`?**
  _High betweenness centrality (0.029) - this node is a cross-community bridge._
- **What connects `AccountScenarioContext`, `AdminUserManagementScenarioContext`, `AttendanceQrScenarioContext` to the rest of the system?**
  _314 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.01 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.01 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.02 - nodes in this community are weakly interconnected._
- **Should `Community 3` be split into smaller, more focused modules?**
  _Cohesion score 0.03 - nodes in this community are weakly interconnected._