# QR Code System Guide

This guide explains the QR code attendance tracking system in the Attendance Management System.

## Overview

The QR code system enables efficient attendance tracking by:
1. Instructors generating QR codes for class sessions
2. Students scanning QR codes to record attendance
3. System validating and recording attendance automatically

## QR Code Flow

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Instructor │     │   System    │     │   Student   │
└──────┬──────┘     └──────┬──────┘     └──────┬──────┘
       │                   │                   │
       │ Generate QR       │                   │
       │──────────────────>│                   │
       │                   │                   │
       │ QR Code Image     │                   │
       │<──────────────────│                   │
       │                   │                   │
       │ Display QR        │                   │
       │─────────────────────────────────────>│
       │                   │                   │
       │                   │     Scan QR       │
       │                   │<──────────────────│
       │                   │                   │
       │                   │ Record Attendance │
       │                   │──────────────────>│
       │                   │                   │
```

## API Endpoints

### Generate QR Code
```http
POST /api/QrCode/generate
Authorization: Bearer {token}
Content-Type: application/json

{
  "sessionId": 1,
  "expirationMinutes": 15
}
```

**Response:**
```json
{
  "id": 1,
  "qrHash": "unique-hash-string",
  "imageBase64": "iVBORw0KGgoAAAANSUhEUgAA...",
  "expiresAt": "2024-01-01T10:15:00Z",
  "sessionId": 1,
  "isActive": true
}
```

### Get QR Code by ID
```http
GET /api/QrCode/{id}
```

### Get QR Code Image
```http
GET /api/QrCode/{id}/image
```
Returns PNG image directly.

### Get QR Codes by Session
```http
GET /api/QrCode/session/{sessionId}
```

### Validate QR Code
```http
GET /api/QrCode/validate/{qrHash}
```
Validates without recording attendance.

### Scan QR Code (Record Attendance)
```http
POST /api/QrCode/scan
Authorization: Bearer {token}
Content-Type: application/json

{
  "qrHash": "unique-hash-string",
  "studentId": 1
}
```

**Response:**
```json
{
  "success": true,
  "message": "Attendance recorded successfully",
  "attendanceRecord": {
    "id": 1,
    "studentId": 1,
    "sessionId": 1,
    "status": "Present",
    "checkInTime": "2024-01-01T10:05:00Z"
  }
}
```

### Revoke QR Code
```http
PATCH /api/QrCode/{id}/revoke
PATCH /api/QrCode/hash/{qrHash}/revoke

{
  "reason": "Session ended early"
}
```

### Reactivate QR Code
```http
PATCH /api/QrCode/{id}/reactivate
PATCH /api/QrCode/hash/{qrHash}/reactivate
```

### Get Scan History
```http
GET /api/QrCode/{id}/scan-history?pageNumber=1&pageSize=50
GET /api/QrCode/hash/{qrHash}/scan-history?pageNumber=1&pageSize=50
```

## QR Code Properties

| Property | Description |
|----------|-------------|
| `id` | Unique identifier |
| `qrHash` | Unique hash for scanning |
| `sessionId` | Associated session |
| `expiresAt` | Expiration timestamp |
| `isActive` | Active status |
| `createdAt` | Creation timestamp |
| `revokedAt` | Revocation timestamp (if revoked) |
| `revocationReason` | Reason for revocation |

## Usage Scenarios

### Regular Class Session
1. Instructor starts a session
2. Generate QR code with 15-minute expiration
3. Display QR code to class
4. Students scan with mobile app
5. Attendance recorded automatically

### Lab Session (Extended)
1. Generate QR code with longer expiration (e.g., 60 minutes)
2. Can generate multiple QR codes if needed
3. Revoke previous QR codes when generating new ones

### Emergency Revocation
1. If QR code is compromised, immediately revoke
2. Generate new QR code
3. Previous scans with revoked code are still valid

## Security Features

### QR Hash
- Cryptographically secure random hash
- Cannot be guessed or predicted
- Unique per QR code

### Expiration
- QR codes expire after specified time
- Expired codes cannot be scanned
- Prevents late check-ins

### Revocation
- Immediate deactivation capability
- Audit trail with reason
- Can be reactivated if needed

### Validation
- QR code must be active
- Must not be expired
- Session must be active
- Student must be enrolled

## Error Handling

| Error | Description |
|-------|-------------|
| `QR_NOT_FOUND` | QR code doesn't exist |
| `QR_EXPIRED` | QR code has expired |
| `QR_REVOKED` | QR code was revoked |
| `SESSION_INACTIVE` | Session is not active |
| `ALREADY_CHECKED_IN` | Student already recorded |
| `NOT_ENROLLED` | Student not in session |

## Best Practices

1. **Set appropriate expiration**: Balance convenience with security
2. **Revoke unused codes**: Clean up after session ends
3. **Monitor scan history**: Check for suspicious activity
4. **Use HTTPS**: Always scan over secure connection
5. **Validate enrollment**: Ensure only enrolled students can scan
