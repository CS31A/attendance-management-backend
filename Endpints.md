# API Endpoints

This document lists all the API endpoints for the attendance monitoring system.

## AccountController

- `POST /api/account/register`: Register a new user account
- `POST /api/account/login`: Authenticate user and return access/refresh tokens
- `POST /api/account/web/login`: Authenticate user and return access/refresh tokens (http-only cookies)
- `POST /api/account/refresh`: Refresh tokens and optionally revoke the old access token
- `POST /api/account/web/refresh`: Refresh tokens using HTTP-only cookies
- `POST /api/account/revoke`: Revoke a refresh token
- `POST /api/account/web/revoke`: Revoke refresh token using HTTP-only cookies
- `GET /api/account/check`: Check if the user is authenticated
- `GET /api/account/me`: Get the current authenticated user's profile information
- `POST /api/account/web/logout`: Logout user by clearing HTTP-only cookies and blacklisting the access token
- `POST /api/account/logout`: Logout user by blacklisting the access token and revoking all refresh tokens
- `PATCH /api/Account/profile`: Update the current user's profile
- `PATCH /api/Account/admin/users/{userId}`: Update any user's profile (admin only)
- `DELETE /api/account/admin/users/{userId}`: Delete a user (soft delete) (admin only) [DEPRECATED - Use UserController endpoints instead]

## AttendanceController

- `POST /api/attendance`: Create a new attendance record manually (Admin, Instructor only). Returns `201 Created` for new records, `200 OK` for idempotent duplicate retries, `409 Conflict` for invalid attendance operations (for example, student not enrolled)
- `GET /api/attendance/{id}`: Get a specific attendance record by ID
- `GET /api/attendance/uuid/{uuid}`: Get a specific attendance record by UUID
- `GET /api/attendance`: Get all attendance records with optional filtering and pagination
- `GET /api/attendance/student/{studentId}`: Get attendance history for a specific student
- `GET /api/attendance/student/uuid/{studentUuid}`: Get attendance history for a specific student by UUID
- `GET /api/attendance/session/{sessionId}`: Get attendance overview for a specific session (Admin, Instructor only) - Requires PrivilegedPolicy authorization
- `GET /api/attendance/session/uuid/{sessionUuid}`: Get attendance overview for a specific session by UUID (Admin, Instructor only) - Requires PrivilegedPolicy authorization
- `GET /api/attendance/summary`: Get attendance summary statistics
- `PUT /api/attendance/{id}`: Update an existing attendance record (Admin, Instructor only)
- `PUT /api/attendance/uuid/{uuid}`: Update an existing attendance record by UUID (Admin, Instructor only)
- `DELETE /api/attendance/{id}`: Delete an attendance record (Admin only)
- `DELETE /api/attendance/uuid/{uuid}`: Delete an attendance record by UUID (Admin only)

## ClassroomController

- `GET /api/classrooms`: Get a list of all classrooms
- `GET /api/classrooms/{id}`: Get a specific classroom by ID
- `POST /api/classrooms`: Create a new classroom
- `PATCH /api/classrooms/{id}`: Update a classroom record
- `DELETE /api/classrooms/{id}`: Delete a classroom by ID

## CourseController

- `GET /api/Course`: Get a list of all courses
- `GET /api/Course/{id}`: Get a specific course by ID
- `POST /api/Course`: Create a new course
- `PUT /api/Course/{id}`: Update a course record
- `DELETE /api/Course/{id}`: Delete a course by ID

## Health Endpoints (Middleware)

- `GET /health/live`: Liveness probe — returns 200 OK when the application is running (no dependencies checked)
- `GET /health/ready`: Readiness probe — checks database connectivity and data integrity; returns per-check statuses
- `GET /health`: Detailed health — runs all registered health checks with durations and diagnostics

## InstructorController

- `GET /api/instructors`: Get a list of all instructors
- `GET /api/instructors/{id}`: Get a specific instructor by ID
- `GET /api/instructors/{instructorId}/subjects`: Get all subjects assigned to a specific instructor
- `GET /api/instructors/profile`: Get the profile of the currently authenticated instructor
- `GET /api/instructors/me/schedules`: Get schedules for the currently authenticated instructor
- `PATCH /api/instructors/{id}`: Update an instructor record
- `PATCH /api/instructors/{id}/soft-delete`: Soft delete an instructor record
- `DELETE /api/instructors/{id}`: Hard delete an instructor record
- `PATCH /api/instructors/{id}/restore`: Restore a soft-deleted instructor record

## QrCodeController

- `POST /api/QrCode/generate`: Generates a new QR code, saves it to database, and returns the PNG image
- `POST /api/QrCode/scan`: Scans a QR code and records attendance for a student
- `GET /api/QrCode/validate/{qrHash}`: Validates a QR code without recording attendance
- `PATCH /api/QrCode/{id}/revoke`: Revokes a QR code by ID
- `PATCH /api/QrCode/hash/{qrHash}/revoke`: Revokes a QR code by hash
- `PATCH /api/QrCode/{id}/reactivate`: Reactivates a previously revoked QR code by ID
- `PATCH /api/QrCode/hash/{qrHash}/reactivate`: Reactivates a previously revoked QR code by hash
- `GET /api/QrCode/{id}`: Gets a QR code by its ID
- `GET /api/QrCode/{id}/image`: Gets a QR code image by its ID
- `GET /api/QrCode/session/{sessionId}`: Gets all QR codes for a specific session
- `GET /api/QrCode/hash/{qrHash}`: Gets a QR code by its hash
- `GET /api/QrCode/{id}/scan-history`: Get scan history for a QR code by ID
- `GET /api/QrCode/hash/{qrHash}/scan-history`: Get scan history for a QR code by hash

## ScheduleController

- `GET /api/schedules`: Get a list of all schedules
- `GET /api/schedules/{id}`: Get a specific schedule by ID
- `GET /api/schedules/{instructorId}/all`: Get all schedules assigned to a specific instructor
- `POST /api/schedules`: Create a new schedule
- `PATCH /api/schedules/{id}`: Update an existing schedule
- `DELETE /api/schedules/{id}`: Delete a schedule by ID

## SectionController

- `GET /api/sections/{id}`: Get a specific section by ID
- `GET /api/sections`: Get a list of all sections
- `POST /api/sections`: Create a new section
- `PUT /api/sections/{id}`: Update a section record
- `DELETE /api/sections/{id}`: Delete a section by ID
- `GET /api/sections/{sectionId}/active-students`: Get a list of all active students in a section
- `GET /api/sections/{sectionId}/all-students`: Get a list of all students (active and inactive) in a section

## SessionController

- `GET /api/sessions`: Get all sessions
- `GET /api/sessions/{id}`: Get a specific session by ID
- `GET /api/sessions/uuid/{uuid}`: Get a specific session by UUID
- `GET /api/sessions/schedule/{scheduleId}`: Get sessions for a specific schedule
- `GET /api/sessions/schedule/uuid/{scheduleUuid}`: Get sessions for a specific schedule by UUID
- `GET /api/sessions/status/{status}`: Get sessions by status (not_started, active, ended, cancelled)
- `GET /api/sessions/date/{date}`: Get sessions for a specific date (YYYY-MM-DD format)
- `POST /api/sessions`: Create a new session for a schedule (Instructor only)
- `PATCH /api/sessions/{id}/start`: Start a session, marking it as active (Instructor only)
- `PATCH /api/sessions/uuid/{uuid}/start`: Start a session by UUID, marking it as active (Instructor only)
- `PATCH /api/sessions/{id}/end`: End an active session (Instructor only)
- `PATCH /api/sessions/uuid/{uuid}/end`: End an active session by UUID (Instructor only)
- `PATCH /api/sessions/{id}/room`: Update the actual room for a session (Instructor only)
- `PATCH /api/sessions/uuid/{uuid}/room`: Update the actual room for a session by UUID (Instructor only)
- `DELETE /api/sessions/{id}`: Cancel a session that has not started yet (Instructor only)
- `DELETE /api/sessions/uuid/{uuid}`: Cancel a session by UUID that has not started yet (Instructor only)

## StudentController

- `GET /api/students`: Get a list of all non-deleted students
- `GET /api/students/{id}`: Get a specific student by ID
- `GET /api/students/my-subjects`: Get subjects for the currently authenticated student
- `GET /api/students/search/name`: Search students by name
- `GET /api/students/search/email`: Search students by email
- `PATCH /api/students/{id}`: Update a student record
- `PATCH /api/students/{id}/soft-delete`: Soft delete a student record
- `DELETE /api/students/{id}`: Hard delete a student record
- `PATCH /api/students/{id}/restore`: Restore a soft-deleted student record

## StudentEnrollmentController

- `POST /api/StudentEnrollment/enroll`: Enroll a student in a section-subject combination (Admin, Instructor only)
- `GET /api/StudentEnrollment/student/{studentId}`: Get all enrollments for a specific student
- `GET /api/StudentEnrollment/section/{sectionId}/students`: Get all active students enrolled in a specific section (Admin, Instructor only)
- `PATCH /api/StudentEnrollment/{enrollmentId}/drop`: Drop a student from a specific enrollment (Admin, Instructor only)
- `PATCH /api/StudentEnrollment/{enrollmentId}/reenroll`: Re-enroll a student (reactivate enrollment) (Admin, Instructor only)
- `GET /api/StudentEnrollment/check`: Check if a student is enrolled in a specific section-subject combination

## SubjectController

- `GET /api/subjects`: Get a list of all subjects
- `GET /api/subjects/{id}`: Get a specific subject by ID
- `POST /api/subjects`: Create a new subject
- `PATCH /api/subjects/{id}`: Update a subject record
- `DELETE /api/subjects/{id}`: Delete a subject by ID

## UserController

- `GET /api/users`: Get all users with their role and profile information (Admin only)
- `DELETE /api/users/{userId}`: Hard delete a user (permanent deletion) (Admin only)
- `PATCH /api/users/{userId}/soft-delete`: Soft delete a user (reversible deletion) (Admin only)
- `PATCH /api/users/{userId}/restore`: Restore a soft-deleted user (Admin only)
