# Authentication & Authorization Guide

This guide provides comprehensive information about the authentication and authorization system in the Attendance Management System.

## Overview

The system implements a dual authentication strategy:
- **JWT Bearer Tokens**: For mobile applications and API clients
- **HTTP-Only Cookies**: For web applications with enhanced security

## Authentication Methods

### JWT Bearer Token Authentication

#### How It Works
1. User provides credentials (username/password)
2. System validates credentials against database
3. JWT access token and refresh token are generated
4. Client includes access token in Authorization header
5. Server validates token on each request

#### Token Structure
```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-id",
    "email": "user@example.com",
    "role": "Student",
    "exp": 1640995200,
    "iss": "AttendanceMonitoringAPI",
    "aud": "AttendanceMonitoringUsers"
  },
  "signature": "HMACSHA256(base64UrlEncode(header) + '.' + base64UrlEncode(payload), secret)"
}
```

#### Usage Example
```http
GET /api/students/profile
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### HTTP-Only Cookie Authentication

#### How It Works
1. User provides credentials via web login
2. System validates credentials
3. Tokens are set as HTTP-only cookies
4. Browser automatically includes cookies in requests
5. Server validates cookies on each request

#### Security Benefits
- **XSS Protection**: JavaScript cannot access HTTP-only cookies
- **CSRF Protection**: SameSite cookie attribute
- **Automatic Management**: Browser handles cookie lifecycle

## Authentication Endpoints

### Registration

#### POST /api/account/register
Register a new user account.

**Request:**
```json
{
  "username": "john.doe",
  "email": "john.doe@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!",
  "role": "Student",
  "firstname": "John",
  "lastname": "Doe",
  "sectionId": 1,
  "isRegular": true
}
```

**Response:**
```json
{
  "success": true,
  "message": "User registered successfully",
  "userId": "guid-here",
  "role": "Student"
}
```

### Login (JWT)

#### POST /api/account/login
Authenticate and receive JWT tokens.

**Request:**
```json
{
  "username": "john.doe",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh-token-here",
  "expiresAt": "2024-01-01T00:15:00Z",
  "user": {
    "id": "guid-here",
    "username": "john.doe",
    "email": "john.doe@example.com",
    "role": "Student"
  }
}
```

### Login (Web/Cookies)

#### POST /api/account/web/login
Authenticate and set HTTP-only cookies.

**Request:** Same as JWT login

**Response:** Same as JWT login, but tokens are set as cookies:
```http
Set-Cookie: AccessToken=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...; HttpOnly; Secure; SameSite=Strict; Path=/
Set-Cookie: RefreshToken=refresh-token-here; HttpOnly; Secure; SameSite=Strict; Path=/api/account
```

### Token Refresh

#### POST /api/account/refresh
Refresh access token using refresh token.

**Request:**
```json
{
  "refreshToken": "refresh-token-here"
}
```

**Response:**
```json
{
  "success": true,
  "accessToken": "new-access-token",
  "refreshToken": "new-refresh-token",
  "expiresAt": "2024-01-01T00:15:00Z"
}
```

### Logout

#### POST /api/account/logout (JWT)
Logout and blacklist tokens.

**Request:**
```json
{
  "refreshToken": "refresh-token-here"
}
```

#### POST /api/account/web/logout (Cookies)
Logout and clear cookies.

**Response:** Clears authentication cookies.

## Authorization System

### Role-Based Access Control

The system implements three primary roles:

#### Student Role
**Permissions:**
- View own profile and attendance records
- Access enrolled subjects and schedules
- Scan QR codes for attendance
- View own attendance history

**Restricted Actions:**
- Cannot access other students' data
- Cannot generate QR codes
- Cannot modify attendance records
- Cannot access administrative functions

#### Teacher Role
**Permissions:**
- View assigned subjects and schedules
- Start and end attendance sessions
- Generate and manage QR codes
- View attendance records for assigned classes
- Manually mark attendance
- Access student lists for assigned sections

**Restricted Actions:**
- Cannot access other teachers' classes
- Cannot modify system-wide settings
- Cannot manage user accounts
- Cannot access administrative reports

#### Admin Role
**Permissions:**
- Full system access
- Manage all users (students, teachers, admins)
- Create and modify courses, sections, subjects
- Access all attendance records and reports
- System configuration and maintenance
- Manage classrooms and schedules

### Authorization Implementation

#### Controller-Level Authorization
```csharp
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    // Admin-only endpoints
}

[Authorize(Roles = "Teacher,Admin")]
public class InstructorController : ControllerBase
{
    // Teacher and Admin endpoints
}
```

#### Action-Level Authorization
```csharp
[HttpGet("profile")]
[Authorize] // Any authenticated user
public async Task<ActionResult> GetProfile()

[HttpPost("generate-qr")]
[Authorize(Roles = "Teacher,Admin")]
public async Task<ActionResult> GenerateQrCode()

[HttpGet("all-users")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult> GetAllUsers()
```

#### Resource-Based Authorization
```csharp
// Students can only access their own data
[HttpGet("{id}")]
[Authorize]
public async Task<ActionResult> GetStudent(int id)
{
    var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var student = await _studentService.GetStudentAsync(id);

    if (student.UserId != currentUserId && !User.IsInRole("Admin"))
    {
        return Forbid();
    }

    return Ok(student);
}
```

## Token Management

### Access Tokens

#### Configuration
```json
{
  "AppSettings": {
    "Token": "your-secret-key-minimum-32-characters",
    "Issuer": "AttendanceMonitoringAPI",
    "Audience": "AttendanceMonitoringUsers"
  },
  "CookieSettings": {
    "AccessTokenExpirationMinutes": 15
  }
}
```

#### Security Features
- **Short Lifespan**: 15-minute default expiration
- **Signed Tokens**: HMAC SHA-256 signature
- **Claims-Based**: User ID, email, role information
- **Validation**: Issuer, audience, and expiration validation

### Refresh Tokens

#### Purpose
- Enable long-lived authentication sessions
- Reduce frequency of credential re-entry
- Provide secure token renewal mechanism

#### Security Features
- **Longer Lifespan**: 7-day default expiration
- **One-Time Use**: Tokens are rotated on each refresh
- **Database Storage**: Hashed tokens stored in database
- **Revocation**: Can be immediately invalidated

#### Refresh Flow
1. Client sends refresh token to `/api/account/refresh`
2. Server validates refresh token against database
3. If valid, new access and refresh tokens are generated
4. Old refresh token is marked as used/revoked
5. New tokens are returned to client

### Token Blacklisting

#### Purpose
- Immediate token revocation for security
- Logout functionality
- Compromised token handling

#### Implementation
```csharp
public class BlacklistedToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; }
    public string UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime BlacklistedAt { get; set; }
}
```

#### Validation Process
1. Extract token from request
2. Check if token hash exists in blacklist
3. If blacklisted, reject request with 401 Unauthorized
4. If not blacklisted, proceed with normal validation

## Security Best Practices

### Token Security

#### Secret Key Management
- **Minimum Length**: 32 characters
- **Cryptographic Strength**: Use secure random generation
- **Environment Variables**: Store in secure configuration
- **Key Rotation**: Periodically rotate signing keys

#### Token Validation
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

### Cookie Security

#### Security Attributes
```csharp
var cookieOptions = new CookieOptions
{
    HttpOnly = true,        // Prevent XSS
    Secure = true,          // HTTPS only
    SameSite = SameSiteMode.Strict,  // CSRF protection
    Expires = DateTime.UtcNow.AddMinutes(15)
};
```

### Password Security

#### Requirements
- Minimum 8 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

#### Implementation
```csharp
services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
});
```

## Error Handling

### Authentication Errors

#### 401 Unauthorized
- Invalid credentials
- Expired tokens
- Blacklisted tokens
- Missing authentication

#### 403 Forbidden
- Insufficient permissions
- Role-based access denied
- Resource-based access denied

### Error Responses
```json
{
  "success": false,
  "message": "Authentication failed",
  "errors": ["Invalid username or password"]
}
```

## Testing Authentication

### Unit Testing
```csharp
[Test]
public async Task Login_ValidCredentials_ReturnsToken()
{
    // Arrange
    var loginDto = new LoginDto
    {
        Username = "testuser",
        Password = "Test123!"
    };

    // Act
    var result = await _accountController.Login(loginDto);

    // Assert
    Assert.IsType<OkObjectResult>(result.Result);
    var response = result.Result as OkObjectResult;
    var loginResponse = response.Value as LoginResponseDto;
    Assert.NotNull(loginResponse.AccessToken);
}
```

### Integration Testing
```csharp
[Test]
public async Task GetProfile_WithValidToken_ReturnsProfile()
{
    // Arrange
    var token = await GetValidTokenAsync();
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await _client.GetAsync("/api/account/me");

    // Assert
    response.EnsureSuccessStatusCode();
    var profile = await response.Content.ReadFromJsonAsync<UserProfileResponseDto>();
    Assert.NotNull(profile);
}
```

This authentication and authorization guide provides comprehensive coverage of the security system in the Attendance Management System.