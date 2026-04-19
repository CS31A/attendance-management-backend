# API Reference

Complete reference for all API endpoints in the Attendance Management System.

## Base URL
- **Development**: `http://localhost:8080/api`
- **Production**: `https://your-domain.com/api`

## Authentication

The API supports two authentication methods:

### JWT Bearer Token (Mobile/API clients)
```http
Authorization: Bearer <access_token>
```

### HTTP-Only Cookies (Web clients)
Tokens are automatically included in cookies for web endpoints.

## Response Format

All API responses follow a consistent format:

### Success Response
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully"
}
```

### Error Response
```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Detailed error messages"]
}
```

## Account Management

### POST /api/account/register
Register a new user account.

**Request Body:**
```json
{
  "username": "string",
  "email": "user@example.com",
  "password": "string",
  "repeatedPassword": "string",
  "role": "Student|Teacher|Instructor|Admin",
  "firstname": "string",
  "lastname": "string",
  "sectionId": 1
}
```

**Response:**
```json
{
  "success": true,
  "message": "User registered successfully",
  "userId": "guid",
  "role": "Student"
}
```

Note: The `role` field can be "Student", "Teacher", or "Admin". "Instructor" is an alias for "Teacher".

### POST /api/account/login
Authenticate user and return JWT tokens.

**Request Body:**
```json
{
  "username": "string",
  "password": "string"
}
```

**Response:**
```json
{
  "success": true,
  "accessToken": "jwt_token",
  "refreshToken": "refresh_token",
  "expiresAt": "2024-01-01T00:00:00Z",
  "user": {
    "id": "guid",
    "username": "string",
    "email": "user@example.com",
    "role": "Student"
  }
}
```

### POST /api/account/web/login
Web-specific login that sets HTTP-only cookies.

**Request Body:**
```json
{
  "identifier": "string",
  "password": "string"
}
```

**Response:** Same as `/login` but tokens are set as HTTP-only cookies.

### POST /api/account/refresh
Refresh access token using refresh token.

**Request Body:**
```json
{
  "refreshToken": "string"
}
```

### GET /api/account/me
Get current user profile information.

**Response:**
```json
{
  "id": "guid",
  "username": "string",
  "email": "user@example.com",
  "role": "Student",
  "studentProfile": {
    "id": 1,
    "firstname": "John",
    "lastname": "Doe",
    "isRegular": true,
    "sectionId": 1,
    "sectionName": "CS-A",
    "courseId": 1,
    "courseName": "Computer Science"
  }
}
```

### POST /api/account/logout
Logout user and invalidate tokens.

### POST /api/account/web/logout
Web-specific logout that clears cookies.

## Student Management

### GET /api/students
Get all non-deleted students.

**Response:**
```json
[
  {
    "id": 1,
    "firstname": "John",
    "lastname": "Doe",
    "isRegular": true,
    "sectionId": 1,
    "userId": "guid",
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
]
```

Note: This endpoint returns a simple array of students without pagination.

### GET /api/students/{id}
Get specific student by ID.

### GET /api/students/search/name
Search for students by name with pagination.

**Query Parameters:**
- `query`: Search term (required)
- `pageNumber`: Page number (default: 1)
- `pageSize`: Items per page (default: 50)

**Response:** Array of students matching search criteria.

### PATCH /api/students/{id}
Update student information.

**Request Body:**
```json
{
  "firstname": "string",
  "lastname": "string",
  "isRegular": true
}
```

### PATCH /api/students/{id}/soft-delete
Soft delete a student (marks as deleted but preserves data).

### PATCH /api/students/{id}/restore
Restore a soft-deleted student.

### DELETE /api/students/{id}
Permanently delete a student (hard delete).

## Instructor Management

### GET /api/instructors
Get all instructors.

### GET /api/instructors/{id}
Get specific instructor by ID.

### GET /api/instructors/profile
Get current instructor's profile (requires Teacher role).

### GET /api/instructors/{instructorId}/subjects
Get all subjects assigned to an instructor.

### PATCH /api/instructors/{id}
Update instructor information.

### PATCH /api/instructors/{id}/soft-delete
Soft delete an instructor.

### PATCH /api/instructors/{id}/restore
Restore a soft-deleted instructor.

## Course Management

### GET /api/Course
Get all courses.

### GET /api/Course/{id}
Get specific course by ID.

### POST /api/Course
Create a new course.

**Request Body:**
```json
{
  "name": "Computer Science",
  "description": "Bachelor of Science in Computer Science",
  "code": "BSCS"
}
```

### PUT /api/Course/{id}
Update course information.

### DELETE /api/Course/{id}
Delete a course.

## Section Management

### GET /api/sections
Get all sections.

### GET /api/sections/{id}
Get specific section by ID.

### GET /api/sections/{sectionId}/active-students
Get all active students in a section.

### GET /api/sections/{sectionId}/all-students
Get all students (active and inactive) in a section.

### POST /api/sections
Create a new section.

**Request Body:**
```json
{
  "name": "CS-A",
  "courseId": 1,
  "capacity": 30
}
```

### PUT /api/sections/{id}
Update section information.

### DELETE /api/sections/{id}
Delete a section.

## Subject Management

### GET /api/subjects
Get all subjects.

### GET /api/subjects/{id}
Get specific subject by ID.

### POST /api/subjects
Create a new subject.

**Request Body:**
```json
{
  "name": "Data Structures",
  "code": "CS201",
  "description": "Introduction to data structures and algorithms",
  "units": 3
}
```

### PATCH /api/subjects/{id}
Update subject information.

### DELETE /api/subjects/{id}
Delete a subject.

## Schedule Management

### GET /api/schedules
Get all schedules.

### GET /api/schedules/{id}
Get specific schedule by ID.

### GET /api/schedules/{instructorId}/all
Get all schedules for a specific instructor.

### POST /api/schedules
Create a new schedule.

**Request Body:**
```json
{
  "subjectId": 1,
  "sectionId": 1,
  "instructorId": 1,
  "classroomId": 1,
  "dayOfWeek": "Monday",
  "startTime": "08:00:00",
  "endTime": "10:00:00"
}
```

### PATCH /api/schedules/{id}
Update schedule information.

### DELETE /api/schedules/{id}
Delete a schedule.

## Classroom Management

### GET /api/classrooms
Get all classrooms.

### GET /api/classrooms/{id}
Get specific classroom by ID.

### POST /api/classrooms
Create a new classroom.

**Request Body:**
```json
{
  "name": "Room 101",
  "building": "Engineering Building",
  "capacity": 40,
  "hasProjector": true,
  "hasWhiteboard": true
}
```

### PATCH /api/classrooms/{id}
Update classroom information.

### DELETE /api/classrooms/{id}
Delete a classroom.

## QR Code System

### POST /api/QrCode/generate
Generate a new QR code for attendance.

**Request Body:**
```json
{
  "scheduleId": 1,
  "expirationMinutes": 15
}
```

**Response:**
```json
{
  "success": true,
  "qrCodeId": 1,
  "qrHash": "unique_hash",
  "imageBase64": "base64_encoded_png",
  "expiresAt": "2024-01-01T00:15:00Z"
}
```

### POST /api/QrCode/scan
Scan a QR code and record attendance for the authenticated student.

**Request Body:**
```json
{
  "qrHash": "unique_hash"
}
```

Legacy clients may still include `studentId`. If provided and it does not match the authenticated student, the scan request is rejected.

**Response:**
```json
{
  "success": true,
  "message": "Attendance marked successfully",
  "attendanceMarked": true,
  "attendanceRecordId": 1,
  "studentName": "Sam Student",
  "className": "CS101 - A",
  "subjectName": "Introduction to Computer Science",
  "roomName": "Room 204",
  "instructorName": "Dr. Jane Smith",
  "attendanceTime": "2024-01-01T10:05:00Z",
  "remainingScans": 2,
  "attendanceStatus": "Present",
  "isDuplicateScan": false
}
```

### PATCH /api/QrCode/{id}/revoke
Revoke a QR code by ID.

### PATCH /api/QrCode/hash/{qrHash}/revoke
Revoke a QR code by hash.

### PATCH /api/QrCode/{id}/reactivate
Reactivate a previously revoked QR code.

## Session Management

### POST /api/sessions/start
Start a new attendance session.

**Request Body:**
```json
{
  "scheduleId": 1,
  "classroomId": 1,
  "sessionType": "Lecture|Lab|Exam"
}
```

### POST /api/sessions/{sessionId}/end
End an active session.

### POST /api/sessions/{sessionId}/cancel
Cancel a session.

### GET /api/sessions/{sessionId}
Get session details.

## Student Enrollment

### GET /api/student-enrollments
Get all student enrollments.

### GET /api/student-enrollments/{id}
Get specific enrollment by ID.

### POST /api/student-enrollments
Create a new student enrollment (for irregular students).

**Request Body:**
```json
{
  "studentId": 1,
  "subjectId": 1,
  "sectionId": 1,
  "enrollmentType": "Regular|Irregular"
}
```

## User Management

Unified user management endpoints for all user types (Admin only).

### GET /api/users
Get all users with role and profile information.

**Query Parameters:**
- `status`: Filter by `Active`, `Archived`, or `All` (default: `Active`)

**Response:**
```json
{
  "users": [
    {
      "id": "guid",
      "email": "user@example.com",
      "role": "Student",
      "isDeleted": false,
      "profile": {
        "id": 1,
        "firstname": "John",
        "lastname": "Doe"
      }
    }
  ]
}
```

### PATCH /api/users/{userId}/soft-delete
Soft delete a user (reversible).

**Response:**
```json
{
  "success": true,
  "message": "User soft deleted successfully"
}
```

### DELETE /api/users/{userId}
Permanently delete a user (cannot be undone).

### PATCH /api/users/{userId}/restore
Restore a soft-deleted user.

## Attendance Management

CRUD operations for attendance records.

### POST /api/attendance
Create a new attendance record manually.

**Request Body:**
```json
{
  "studentId": 1,
  "sessionId": 1,
  "status": "Present|Late|Absent|Excused",
  "notes": "Optional notes"
}
```

**Response:**
```json
{
  "id": 1,
  "studentId": 1,
  "sessionId": 1,
  "status": "Present",
  "checkInTime": "2024-01-01T10:00:00Z"
}
```

**Status behavior:**
- `201 Created` when a new attendance record is created
- `200 OK` when the request is an idempotent duplicate retry for the same student/session and the existing record is returned
- `409 Conflict` for invalid attendance operations (for example, student not enrolled in the session)

### GET /api/attendance/{id}
Get a specific attendance record.

### GET /api/attendance
Get all attendance records with optional filtering.

**Query Parameters:**
- `studentId`: Filter by student
- `sessionId`: Filter by session
- `status`: Filter by attendance status
- `page`: Page number
- `pageSize`: Items per page

### GET /api/attendance/student/{studentId}/history
Get attendance history for a specific student.

**Response:**
```json
{
  "studentId": 1,
  "history": [...],
  "statistics": {
    "totalSessions": 20,
    "present": 18,
    "late": 1,
    "absent": 1,
    "attendanceRate": 0.95
  }
}
```

### GET /api/attendance/session/{sessionId}
Get attendance overview for a session.

### GET /api/attendance/summary
Get attendance summary statistics.

### PATCH /api/attendance/{id}
Update an attendance record.

**Request Body:**
```json
{
  "status": "Late",
  "notes": "Arrived 10 minutes late"
}
```

### DELETE /api/attendance/{id}
Delete an attendance record (Admin only).

## Notification Preferences

Manage notification preferences (Instructor only).

### GET /api/notificationpreference/realtime-checkin
Get current real-time check-in notification preference.

**Response:**
```json
{
  "enabled": true,
  "message": "You will receive real-time notifications when students check in"
}
```

### PUT /api/notificationpreference/realtime-checkin
Update real-time check-in notification preference.

**Request Body:**
```json
{
  "enabled": false
}
```

## Health Check


### GET /api/health
Check API and database health.

**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-01T00:00:00Z",
  "database": "Connected",
  "version": "1.0.0"
}
```

## Error Codes

| Code | Description |
|------|-------------|
| 400 | Bad Request - Invalid input data |
| 401 | Unauthorized - Authentication required |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource not found |
| 409 | Conflict - Invalid operation for current state (for example, attendance create with unenrolled student) |
| 422 | Unprocessable Entity - Validation failed |
| 500 | Internal Server Error - Server error |

## Rate Limiting

- **Authentication endpoints**: 5 requests per minute per IP
- **General endpoints**: 100 requests per minute per user
- **QR code generation**: 10 requests per minute per instructor

## Pagination

List endpoints support pagination with the following parameters:
- `page`: Page number (1-based, default: 1)
- `pageSize`: Items per page (max: 100, default: 10)

Response includes pagination metadata:
```json
{
  "data": [...],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalCount": 100,
    "totalPages": 10,
    "hasNext": true,
    "hasPrevious": false
  }
}
```
