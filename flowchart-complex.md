# Attendance Management Backend - Complex Data Flow Chart

## Detailed Architecture with All Layers, Middleware, Error Handling, and Background Services

---

## Complete Request Pipeline Flow

```mermaid
flowchart TD
    subgraph Client["Client Application"]
        WEB[Web App]
        MOBILE[Mobile App]
    end

    subgraph Middleware["Middleware Pipeline (Ordered)"]
        COMP[Selective Response Compression<br/>GET only, specific endpoints]
        PERF[Performance Monitoring<br/>X-Response-Time header]
        DEV[Development Tools<br/>Swagger/Scalar - Dev only]
        STATIC[Static Files]
        HTTPS[HTTPS Redirection<br/>Dev only]
        CORS[CORS Policy<br/>AllowFrontend]
        AUTH[Authentication<br/>JWT Validation]
        AUTHZ[Authorization<br/>Role-Based Policies]
        CTRL[Controller Mapping]
    end

    subgraph ExceptionHandler["Global Exception Handler"]
        GLOBAL_CATCH[Global Exception Middleware]
        ERROR_RESPONSE[Standardized Error Response]
    end

    subgraph SignalR["SignalR Real-Time Notifications"]
        HUB[NotificationHub]
        GROUPS[Session Groups & Role Groups]
    end

    WEB --> Middleware
    MOBILE --> Middleware

    COMP --> PERF
    PERF --> DEV
    DEV --> STATIC
    STATIC --> HTTPS
    HTTPS --> CORS
    CORS --> AUTH
    AUTH --> AUTHZ
    AUTHZ --> CTRL

    CTRL -->|Route Match| Controllers
    CTRL -->|No Match| GLOBAL_CATCH

    Controllers -->|Error| GLOBAL_CATCH
    Controllers -->|Success| Response

    GLOBAL_CATCH --> ERROR_RESPONSE
    ERROR_RESPONSE --> Response

    Response[HTTP Response] --> WEB
    Response --> MOBILE

    Controllers -->|Notification| HUB
    HUB --> GROUPS
    GROUPS --> WEB
    GROUPS --> MOBILE
```

---

## 1. Complete Registration Flow with Error Handling

```mermaid
flowchart TD
    subgraph Client["Client"]
        POST_REG[POST /api/account/register<br/>RegisterDto]
    end

    subgraph Middleware["Middleware Pipeline"]
        M1[Response Compression]
        M2[Performance Monitoring]
        M3[Development Tools]
        M4[HTTPS Redirection]
        M5[CORS]
        M6[Authentication - Skip]
        M7[Authorization - Skip]
    end

    subgraph Controller["AccountController"]
        AC1[Register Endpoint]
        VALIDATE_MODEL{ModelState<br/>Valid?}
    end

    subgraph Service["AccountService"]
        AS1[RegisterAsync]
        NORMALIZE_ROLE{Normalize Role<br/>Instructor→Teacher}
        CHECK_DUPLICATE{Check Duplicate<br/>Username/Email}
        CHECK_SECTION{Section Exists?<br/>For Students}
        CREATE_USER[Create IdentityUser]
        HASH_PW[Hash Password]
        CREATE_ROLE[Create Role Entity]
        SAVE_TRANSACTION[Database Transaction]
    end

    subgraph Repository["AccountRepository"]
        AR1[FindByUsernameAsync]
        AR2[CreateAsync]
        AR3[AssignRoleAsync]
    end

    subgraph Database["ApplicationDbContext"]
        DB_USERS[AspNetUsers Table]
        DB_ROLES[AspNetRoles Table]
        DB_STUDENT[Students Table]
        DB_INSTRUCTOR[Instructors Table]
        DB_ADMIN[Admins Table]
    end

    subgraph ErrorHandling["Exception Handling"]
        E1[EntityAlreadyExistsException<br/>→ 409 Conflict]
        E2[EntityNotFoundException<br/>→ 404 Not Found]
        E3[ValidationException<br/>→ 400 Bad Request]
        E4[EntityServiceException<br/>→ 400 Bad Request]
        E5[Global Exception Handler<br/>→ 500 Internal Server Error]
    end

    POST_REG --> M1 --> M2 --> M3 --> M4 --> M5 --> M6 --> M7 --> AC1

    AC1 --> VALIDATE_MODEL
    VALIDATE_MODEL -->|Invalid| RETURN_400[Return 400 Bad Request]
    VALIDATE_MODEL -->|Valid| AS1

    AS1 --> NORMALIZE_ROLE --> CHECK_DUPLICATE
    CHECK_DUPLICATE -->|Exists| E1
    CHECK_DUPLICATE -->|Unique| CHECK_SECTION

    CHECK_SECTION -->|Not Found| E2
    CHECK_SECTION -->|Found| CREATE_USER

    CREATE_USER --> HASH_PW --> CREATE_ROLE --> SAVE_TRANSACTION

    SAVE_TRANSACTION --> AR1
    AR1 --> DB_USERS
    SAVE_TRANSACTION --> AR2
    AR2 --> DB_STUDENT
    SAVE_TRANSACTION --> AR3
    AR3 --> DB_ROLES

    SAVE_TRANSACTION -->|Success| RETURN_200[Return 200 OK<br/>RegisterResponseDto]

    E1 --> RETURN_409[Return 409 Conflict]
    E2 --> RETURN_404[Return 404 Not Found]
    E3 --> RETURN_400V[Return 400 Bad Request]
    E4 --> RETURN_400S[Return 400 Bad Request]
```

---

## 2. Complete QR Code Scan Flow (Attendance Recording)

```mermaid
flowchart TD
    subgraph Client["Student Client"]
        POST_SCAN[POST /api/qrcode/scan<br/>ValidateQrCode Request<br/>qrHash + studentId]
    end

    subgraph Middleware["Middleware Pipeline"]
        MW1[Compression]
        MW2[Performance Monitoring]
        MW3[Development Tools]
        MW4[HTTPS]
        MW5[CORS]
        MW6[Authentication Required<br/>Validate JWT]
        MW7[Authorization<br/>UserPolicy]
    end

    subgraph Controller["QrCodeController"]
        QC1[ScanQrCode Endpoint]
    end

    subgraph Service["QrCodeService"]
        QS1[ScanQrCodeAsync]
    end

    subgraph QRValidation["QR Code Validation"]
        V1[QR Exists?]
        V2[QR Active?<br/>IsActive=true]
        V3[QR Not Expired?<br/>ExpiresAt > UtcNow]
        V4[Usage Limit Not Reached?<br/>UsageCount < MaxUsage]
        V5[QR Not Revoked?<br/>RevokedAt is null]
    end

    subgraph StudentValidation["Student Validation"]
        S1[Student Exists?]
        S2[Student Not Deleted?<br/>IsDeleted=false]
        S3[Student Enrolled?<br/>In Section/Subject]
    end

    subgraph DuplicateCheck["Duplicate Prevention"]
        D1[Check Existing Attendance<br/>HasAttendanceRecordAsync]
        D2[Unique Constraint Check<br/>StudentId + SessionId]
    end

    subgraph AtomicOperations["Atomic Operations"]
        A1[Increment Usage Count<br/>Optimistic Concurrency]
        A2[Create AttendanceRecord]
        A3[Determine Attendance Status]
    end

    subgraph StatusLogic["Attendance Status Determination"]
        ST1{CheckInTime vs<br/>SessionStartTime}
        ST1 -->|On Time| PRESENT[Status: Present]
        ST1 -->|Within 15 min| LATE[Status: Late]
        ST1 -->|After 15 min| LATE2[Status: Late<br/>Instructor can change]
    end

    subgraph Notification["SignalR Notification"]
        N1[NotifyStudentCheckedInAsync]
        N2[Add to Session Group]
        N3[Send to Instructor]
        N4[Send to Student<br/>If opted-in]
    end

    subgraph Database["Database Operations"]
        DB_QR[QrCodes Table<br/>RowVersion for concurrency]
        DB_ATT[AttendanceRecords Table<br/>Unique Index]
        DB_STUD[Students Table]
        DB_SESS[Sessions Table]
    end

    subgraph Responses["Response Handling"]
        R_SUCCESS[Success Response<br/>200 OK]
        R_DUP[Duplicate Scan<br/>200 OK with isDuplicate=true]
        R_QR_INVALID[QR Invalid<br/>200 OK with success=false]
        R_NOT_ENROLLED[Not Enrolled<br/>403 Forbidden]
    end

    POST_SCAN --> MW1 --> MW2 --> MW3 --> MW4 --> MW5 --> MW6 --> MW7 --> QC1 --> QS1

    QS1 --> V1
    V1 -->|Not Found| R_QR_INVALID
    V1 -->|Found| V2
    V2 -->|Inactive| R_QR_INVALID
    V2 -->|Active| V3
    V3 -->|Expired| R_QR_INVALID
    V3 -->|Valid| V4
    V4 -->|Limit Reached| R_QR_INVALID
    V4 -->|Available| V5
    V5 -->|Revoked| R_QR_INVALID
    V5 -->|Valid| S1

    S1 -->|Not Found| R_QR_INVALID
    S1 -->|Found| S2
    S2 -->|Deleted| R_QR_INVALID
    S2 -->|Active| S3
    S3 -->|Not Enrolled| R_NOT_ENROLLED
    S3 -->|Enrolled| D1

    D1 --> D2
    D1 -->|Exists| R_DUP
    D1 -->|New| A1

    A1 --> DB_QR
    A1 --> A2
    A2 --> DB_ATT
    A2 --> A3

    A3 --> ST1
    ST1 --> PRESENT
    ST1 --> LATE
    ST1 --> LATE2

    A2 --> N1
    N1 --> N2
    N1 --> N3
    N1 --> N4

    A2 --> R_SUCCESS
```

---

## 3. QR Code Generation Flow (Instructor/Admin)

```mermaid
flowchart TD
    subgraph Client["Instructor/Admin Client"]
        POST_GEN[POST /api/qrcode/generate<br/>QrCodeRequest<br/>sessionId + options]
    end

    subgraph Middleware["Middleware Pipeline"]
        MW1[Compression]
        MW2[Performance Monitoring]
        MW3[Development Tools]
        MW4[HTTPS]
        MW5[CORS]
        MW6[Authentication<br/>Validate JWT]
        MW7[Authorization<br/>PrivilegedPolicy<br/>Admin/Instructor Only]
    end

    subgraph Controller["QrCodeController"]
        QC1[GenerateQrCode Endpoint]
    end

    subgraph Service["QrCodeService"]
        QS1[GenerateQrCodeAsync]
    end

    subgraph Validation["Validation"]
        V1[Session Exists?]
        V2[Session is Active?<br/>Status=active]
        V3[User Authorized?<br/>Owns Session or Admin]
    end

    subgraph Generation["QR Generation"]
        G1[Generate Unique Hash]
        G2[Set Expiration<br/>Default or Custom]
        G3[Set Max Usage<br/>Default or Custom]
    end

    subgraph ImageGeneration["Image Generation"]
        I1[QRCoder Library]
        I2[CreateQrCode with ECCLevel.Q]
        I3[PngByteQRCode]
        I4[GetGraphic - 20px scale]
        I5[Convert to Base64]
    end

    subgraph Database["Database Operations"]
        DB_SAVE[Save QrCode Entity<br/>with RowVersion]
    end

    subgraph Response["Response"]
        R[200 OK<br/>qrCodeId, qrHash,<br/>qrCodeImage base64,<br/>generatedAt, expiresAt,<br/>maxUsage]
    end

    POST_GEN --> MW1 --> MW2 --> MW3 --> MW4 --> MW5 --> MW6 --> MW7 --> QC1

    MW7 -->|Unauthorized| FORBIDDEN[403 Forbidden]

    QC1 --> QS1
    QS1 --> V1
    V1 -->|Not Found| NOT_FOUND[404 Not Found]
    V1 -->|Found| V2
    V2 -->|Not Active| BAD_REQUEST[400 Bad Request]
    V2 -->|Active| V3
    V3 -->|Not Authorized| FORBIDDEN
    V3 -->|Authorized| G1

    G1 --> G2 --> G3 --> DB_SAVE

    DB_SAVE --> I1 --> I2 --> I3 --> I4 --> I5

    I5 --> R
```

---

## 4. Login Flow with Token Management

```mermaid
flowchart TD
    subgraph Client["Client"]
        POST_LOGIN[POST /api/account/login<br/>LoginDto<br/>username + password]
    end

    subgraph Middleware["Middleware Pipeline"]
        MW1[Compression]
        MW2[Performance Monitoring]
        MW3[Development Tools]
        MW4[HTTPS]
        MW5[CORS]
        MW6[Authentication - Skip]
        MW7[Authorization - Skip]
    end

    subgraph Controller["AccountController"]
        AC1[Login Endpoint]
        VALIDATE[Validate ModelState]
    end

    subgraph Service["AccountService"]
        AS1[LoginAsync]
        FIND_USER[Find User by Username]
        VERIFY_PW[Verify Password<br/>BCrypt Verify]
        CHECK_LOCKOUT[Check Account Lockout]
        GEN_ACCESS[Generate JWT Access Token<br/>15min expiration]
        GEN_REFRESH[Generate Refresh Token<br/>7 days expiration]
        SAVE_REFRESH[Save RefreshToken<br/>to Database]
        BLACKLIST_CHECK[Check Token Blacklist]
    end

    subgraph TokenGeneration["Token Generation"]
        TG1[Create Claims<br/>UserId, Username, Role]
        TG2[Signing Credentials<br/>Symmetric Key]
        TG3[Token Descriptor<br/>Expires, SigningKey]
        TG4[JwtSecurityTokenHandler]
    end

    subgraph Database["Database Operations"]
        DB_USER[AspNetUsers Table]
        DB_REFRESH[RefreshTokens Table]
        DB_BLACKLIST[BlacklistedTokens Table]
    end

    subgraph Response["Response"]
        R_200[200 OK<br/>AccessToken, RefreshToken,<br/>Username, Role]
        R_401[401 Unauthorized<br/>Invalid credentials]
    end

    POST_LOGIN --> MW1 --> MW2 --> MW3 --> MW4 --> MW5 --> MW6 --> MW7 --> AC1

    AC1 --> VALIDATE
    VALIDATE -->|Invalid| BAD_REQ[400 Bad Request]
    VALIDATE -->|Valid| AS1

    AS1 --> FIND_USER
    FIND_USER -->|Not Found| R_401
    FIND_USER -->|Found| VERIFY_PW

    VERIFY_PW -->|Invalid| R_401
    VERIFY_PW -->|Valid| CHECK_LOCKOUT

    CHECK_LOCKOUT -->|Locked| R_401
    CHECK_LOCKOUT -->|Not Locked| BLACKLIST_CHECK

    BLACKLIST_CHECK --> GEN_ACCESS

    GEN_ACCESS --> TG1 --> TG2 --> TG3 --> TG4

    GEN_REFRESH --> SAVE_REFRESH --> DB_REFRESH

    GEN_ACCESS --> R_200
    GEN_REFRESH --> R_200
```

---

## 5. Background Services Flow

```mermaid
flowchart TD
    subgraph Host["ASP.NET Core Host"]
        START[Application Start]
    end

    subgraph BackgroundServices["Background Services"]
        BS1[BlacklistedTokenCleanupService<br/>Runs every 24 hours]
        BS2[RoleInitializationBackgroundService<br/>Runs once on startup]
        BS3[OrphanedUserCleanupService<br/>Runs periodically]
    end

    subgraph Cleanup1["Token Cleanup"]
        C1[Query Expired Blacklisted Tokens]
        C2[Delete from Database]
    end

    subgraph InitRoles["Role Initialization"]
        R1[Check if Roles Exist]
        R2[Create Admin Role]
        R3[Create Teacher Role]
        R4[Create Student Role]
    end

    subgraph CleanupOrphans["Orphaned User Cleanup"]
        O1[Find Users without<br/>Role Entity]
        O2[Soft Delete Orphaned Users]
    end

    START --> BS1
    START --> BS2
    START --> BS3

    BS1 --> C1 --> C2
    BS2 --> R1 --> R2 --> R3 --> R4
    BS3 --> O1 --> O2
```

---

## Authorization Policy Flow

```mermaid
flowchart TD
    subgraph Policies["Authorization Policies"]
        P1[AdminPolicy<br/>Role: Admin]
        P2[PrivilegedPolicy<br/>Role: Admin OR Teacher]
        P3[UserPolicy<br/>Authenticated Users]
    end

    subgraph Endpoints["Protected Endpoints"]
        E1[POST /api/qrcode/generate<br/>PrivilegedPolicy]
        E2[POST /api/attendance<br/>PrivilegedPolicy]
        E3[GET /api/students<br/>PrivilegedPolicy]
        E4[PATCH /api/account/admin/users/{id}<br/>AdminPolicy]
        E5[GET /api/students/my-subjects<br/>UserPolicy]
        E6[POST /api/qrcode/scan<br/>Authorized]
    end

    P1 --> E4
    P2 --> E1
    P2 --> E2
    P2 --> E3
    P3 --> E5
    Auth --> E6
```

---

## Database Schema Relationships

```mermaid
erDiagram
    Student ||--o{ AttendanceRecord : "has"
    Instructor ||--o{ Schedule : "teaches"
    Schedule ||--o{ Session : "defines"
    Session ||--o{ QrCode : "uses"
    Session ||--o{ AttendanceRecord : "records"
    QrCode ||--o{ AttendanceRecord : "creates"
    Section ||--o{ StudentEnrollment : "contains"
    Subject ||--o{ StudentEnrollment : "offers"
    Student ||--o{ StudentEnrollment : "enrolled in"
    Student ||--|| IdentityUser : "linked to"
    Instructor ||--|| IdentityUser : "linked to"
    Admin ||--|| IdentityUser : "linked to"
    Course ||--o{ Subject : "contains"
    Section ||--o{ Schedule : "has"
    Classroom ||--o{ Session : "hosts"

    Student {
        int Id PK
        string UserId FK
        string FirstName
        string LastName
        string StudentNumber
        bool IsDeleted
    }

    AttendanceRecord {
        int Id PK
        int StudentId FK
        int SessionId FK
        int? QrCodeId FK
        DateTime CheckInTime
        string Status
        bool IsManualEntry
    }

    QrCode {
        int Id PK
        int SessionId FK
        string QrHash UK
        DateTime GeneratedAt
        DateTime ExpiresAt
        int MaxUsage
        int UsageCount
        bool IsActive
        byte[] RowVersion
    }

    Session {
        int Id PK
        int ScheduleId FK
        int? ActualRoomId FK
        DateTime SessionDate
        string Status
    }
```

---

## Error Handling Hierarchy

```mermaid
flowchart TD
    subgraph Exceptions["Custom Exceptions"]
        E1[EntityNotFoundException<br/>→ 404]
        E2[EntityAlreadyExistsException<br/>→ 409 (resource-specific)]
        E3[EntityUnauthorizedException<br/>→ 403]
        E4[EntityServiceException<br/>→ 400]
        E5[ValidationException<br/>→ 400]
    end

    subgraph GlobalHandler["Global Exception Middleware"]
        GH1[Catch Exception]
        GH2[Log Error]
        GH3[Map to HTTP Status]
        GH4[Return Error Response]
    end

    Exceptions --> GH1
    GH1 --> GH2 --> GH3 --> GH4
```

---

## API Endpoint Summary

### Authentication Endpoints
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/account/register | None | Register new user |
| POST | /api/account/login | None | Login with JWT |
| POST | /api/account/refresh | None | Refresh access token |
| POST | /api/account/logout | Authorized | Logout and blacklist |
| GET | /api/account/me | Authorized | Get user profile |
| PATCH | /api/account/profile | Authorized | Update own profile |

### Student Endpoints
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | /api/students | Privileged | List all students |
| GET | /api/students/{id} | Privileged | Get student details |
| PATCH | /api/students/{id} | Privileged | Update student |
| PATCH | /api/students/{id}/soft-delete | Admin | Soft delete student |
| GET | /api/students/my-subjects | Authorized | Get student's subjects |
| GET | /api/students/search/name | Privileged | Search by name |

### QR Code Endpoints
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/qrcode/generate | Privileged | Generate QR code |
| POST | /api/qrcode/scan | Authorized | Scan QR code |
| GET | /api/qrcode/validate/{hash} | None | Validate QR code |
| PATCH | /api/qrcode/{id}/revoke | Privileged | Revoke QR code |
| GET | /api/qrcode/{id}/scan-history | Privileged | Get scan history |

### Attendance Endpoints
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/attendance | Privileged | Manual attendance |
| GET | /api/attendance/{id} | Privileged | Get attendance record |
| GET | /api/attendance/student/{id} | Privileged | Student history |
| GET | /api/attendance/session/{id} | Privileged | Session overview |
| GET | /api/attendance/summary | Privileged | Statistics |
