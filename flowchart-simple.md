# Attendance Management Backend - Simple Data Flow Chart

## Overview
This document provides a high-level view of the main data flows through the Attendance Management Backend application.

## Architecture Pattern
**Layered Architecture** with Clean Architecture principles
- Presentation Layer (Controllers)
- Business Logic Layer (Services)
- Data Access Layer (Repositories)
- Data Layer (Entity Framework/Database)

---

## Main Data Flows

### 1. User Registration Flow

```mermaid
flowchart TD
    A[Client Request<br/>POST /api/account/register] --> B[AccountController]
    B --> C{Validate Model}
    C -->|Invalid| D[Return 400 Bad Request]
    C -->|Valid| E[AccountService.RegisterAsync]
    E --> F[Create IdentityUser<br/>AccountRepository]
    F --> G[Create Role Entity<br/>Student/Instructor/Admin]
    G --> H[Save to Database]
    H --> I[Return 200 OK<br/>RegisterResponseDto]
```

### 2. Login & Authentication Flow

```mermaid
flowchart TD
    A[Client Request<br/>POST /api/account/login] --> B[AccountController]
    B --> C{Validate Credentials}
    C -->|Invalid| D[Return 401 Unauthorized]
    C -->|Valid| E[AccountService.LoginAsync]
    E --> F[Generate JWT Access Token]
    E --> G[Generate Refresh Token]
    E --> H[Store Refresh Token<br/>in Database]
    F --> I[Return 200 OK<br/>LoginResponseDto]
    G --> I
```

### 3. Student Attendance via QR Code Scan (Primary Flow)

```mermaid
flowchart TD
    A[Student Scans QR Code<br/>POST /api/qrcode/scan] --> B[QrCodeController]
    B --> C[QrCodeService.ScanQrCodeAsync]
    C --> D{Validate QR Code}
    D -->|Not Found/Expired/Inactive| E[Return Error]
    D -->|Valid| F{Validate Student<br/>Enrollment}
    F -->|Not Enrolled| G[Return Error]
    F -->|Enrolled| H{Check for<br/>Duplicate}
    H -->|Duplicate| I[Return Duplicate Warning]
    H -->|New| J[Increment Usage Count]
    J --> K[Create AttendanceRecord]
    K --> L[Determine Status<br/>Present/Late]
    L --> M[Send SignalR Notification]
    M --> N[Return 200 OK<br/>ScanResponseDto]
```

### 4. QR Code Generation Flow (Instructor/Admin)

```mermaid
flowchart TD
    A[Instructor Request<br/>POST /api/qrcode/generate] --> B[QrCodeController]
    B --> C{Authorize<br/>PrivilegedPolicy}
    C -->|Unauthorized| D[Return 403 Forbidden]
    C -->|Authorized| E[QrCodeService.GenerateQrCodeAsync]
    E --> F[Validate Session]
    F --> G[Generate QR Hash]
    G --> H[Create QrCode Entity]
    H --> I[Save to Database]
    I --> J[Generate QR Image PNG]
    J --> K[Return 200 OK<br/>QR Code with Metadata]
```

### 5. Manual Attendance Recording (Admin/Instructor)

```mermaid
flowchart TD
    A[Admin/Instructor Request<br/>POST /api/attendance] --> B[AttendanceController]
    B --> C[AttendanceService.CreateAsync]
    C --> D{Validate Student & Session}
    D -->|Invalid| E[Return 400 Bad Request]
    D -->|Valid| F[Create AttendanceRecord<br/>IsManualEntry=true]
    F --> G[Save to Database]
    G --> H[Return 200 OK<br/>AttendanceRecordDto]
```

---

## General Request Flow Pattern

```mermaid
flowchart LR
    Client[Client Application] --> Middleware[Middleware Pipeline]
    Middleware --> Auth[Authentication]
    Auth --> Authorize[Authorization]
    Authorize --> Controller[API Controller]
    Controller --> Service[Service Layer]
    Service --> Repository[Repository]
    Repository --> DB[(Database)]
    DB --> Repository
    Repository --> Service
    Service --> Controller
    Controller --> Client
```

---

## Key Entry Points Summary

| Entry Point | Purpose | Flow |
|-------------|---------|------|
| `/api/account/register` | User Registration | Account → AccountService → AccountRepository → DB |
| `/api/account/login` | User Login | Account → AccountService → Token Generation |
| `/api/qrcode/generate` | Generate QR Code | QrCode → QrCodeService → QrCodeRepository → DB |
| `/api/qrcode/scan` | Mark Attendance | QrCode → QrCodeService → AttendanceRepository → DB |
| `/api/attendance` | Manual Attendance | Attendance → AttendanceService → AttendanceRepository → DB |
| `/api/students` | Student Management | Student → StudentService → StudentRepository → DB |

---

## Data Entities

| Entity | Purpose |
|--------|---------|
| **Student** | Student profile information |
| **Instructor** | Instructor/Teacher profile |
| **Admin** | Administrator profile |
| **AttendanceRecord** | Attendance tracking (Present/Late/Absent/Excused) |
| **QrCode** | QR codes for session check-in |
| **Session** | Scheduled class sessions |
| **Schedule** | Recurring schedule patterns |
| **StudentEnrollment** | Student-section-subject relationships |
| **Subject** | Academic subjects |
| **Course** | Courses containing subjects |
| **Section** | Class sections |
| **Classroom** | Physical classroom locations |
