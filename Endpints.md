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

## HealthCheckController

- `GET /api/health`: Check the health of the API and database connection

## InstructorController

- `GET /api/instructors`: Get a list of all instructors
- `GET /api/instructors/{id}`: Get a specific instructor by ID
- `GET /api/instructors/{instructorId}/subjects`: Get all subjects assigned to a specific instructor
- `GET /api/instructors/profile`: Get the profile of the currently authenticated instructor
- `PATCH /api/instructors/{id}`: Update an instructor record
- `PATCH /api/instructors/{id}/soft-delete`: Soft delete an instructor record
- `DELETE /api/instructors/{id}`: Hard delete an instructor record
- `PATCH /api/instructors/{id}/restore`: Restore a soft-deleted instructor record

## QrCodeController

- `POST /api/QrCode/generate`: Generates a new QR code, saves it to database, and returns the PNG image
- `PATCH /api/QrCode/{id}/revoke`: Revokes a QR code by ID
- `PATCH /api/QrCode/hash/{qrHash}/revoke`: Revokes a QR code by hash
- `PATCH /api/QrCode/{id}/reactivate`: Reactivates a previously revoked QR code by ID
- `PATCH /api/QrCode/hash/{qrHash}/reactivate`: Reactivates a previously revoked QR code by hash

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

## StudentController

- `GET /api/students`: Get a list of all non-deleted students
- `GET /api/students/{id}`: Get a specific student by ID
- `PATCH /api/students/{id}`: Update a student record
- `PATCH /api/students/{id}/soft-delete`: Soft delete a student record
- `DELETE /api/students/{id}`: Hard delete a student record
- `PATCH /api/students/{id}/restore`: Restore a soft-deleted student record

## SubjectController

- `GET /api/subjects`: Get a list of all subjects
- `GET /api/subjects/{id}`: Get a specific subject by ID
- `POST /api/subjects`: Create a new subject
- `PATCH /api/subjects/{id}`: Update a subject record
- `DELETE /api/subjects/{id}`: Delete a subject by ID

