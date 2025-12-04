using System.Text.Json;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using attendance_monitoring.Constants;

namespace attendance_monitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrCodeController(
    IQrCodeService qrCodeService,
    ISessionRepository sessionRepository,
    UserContextService userContextService,
    ILogger<QrCodeController> logger) : ControllerBase
{
    /// <summary>
    /// Generates a new QR code, saves it to database, and returns the QR code data with metadata.
    /// </summary>
    /// <param name="request">QR code generation parameters.</param>
    /// <returns>QR code metadata including database ID and base64-encoded PNG image.</returns>
    [HttpPost("generate")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult> GenerateQrCode([FromBody] QrCodeRequest request)
    {
        try
        {
            logger.LogInformation("Generating QR code for session ID: {SessionId}",
                request.SessionId);

            // Generate and save QR code to database
            var result = await qrCodeService.GenerateQrCodeAsync(request, User);

            if (!result.Success)
            {
                logger.LogWarning("Failed to generate QR code: {Message}", result.Message);
                return BadRequest(new { message = result.Message });
            }

            // Serialize the QR hash as the data to encode
            var qrData = result.QrHash;

            // Generate QR Code image from the hash
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);

            // Get PNG as byte array
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            logger.LogInformation("Successfully generated QR code with ID: {QrCodeId}, hash: {QrHash}",
                result.QrCodeId, result.QrHash);

            // Return JSON response with image as base64 and all metadata
            return Ok(new
            {
                success = result.Success,
                message = result.Message,
                qrCodeId = result.QrCodeId,
                qrHash = result.QrHash,
                qrCodeData = result.QrCodeData,
                qrCodeImage = Convert.ToBase64String(qrCodeImage),
                generatedAt = result.GeneratedAt,
                expiresAt = result.ExpiresAt,
                maxUsage = result.MaxUsage
            });
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Entity not found while generating QR code");
            return NotFound(new { message = ex.Message });
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning(ex, "Unauthorized QR code generation attempt");
            return Forbid();
        }
        // No generic catch - global handler will manage unexpected errors
    }

    /// <summary>
    /// Revokes a QR code by ID (deactivates with audit trail).
    /// </summary>
    /// <param name="id">The QR code ID to revoke.</param>
    /// <param name="request">Optional revocation reason.</param>
    /// <returns>NoContent if successful, error otherwise.</returns>
    [HttpPatch("{id}/revoke")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult> RevokeQrCode(int id, [FromBody] RevokeQrCode? request)
    {
        logger.LogInformation("Revoking QR code with ID: {QrCodeId}", id);

        var error = await qrCodeService.RevokeQrCodeAsync(id, request?.Reason, User);
        if (error != null)
        {
            logger.LogWarning("Failed to revoke QR code with ID {QrCodeId}: {Error}", id, error);
            return BadRequest(new { message = error });
        }

        logger.LogInformation("Successfully revoked QR code with ID: {QrCodeId}", id);
        return NoContent();
    }

    /// <summary>
    /// Revokes a QR code by hash (deactivates with audit trail).
    /// </summary>
    /// <param name="qrHash">The QR code hash to revoke.</param>
    /// <param name="request">Optional revocation reason.</param>
    /// <returns>NoContent if successful, error otherwise.</returns>
    [HttpPatch("hash/{qrHash}/revoke")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult> RevokeQrCodeByHash(string qrHash, [FromBody] RevokeQrCode? request)
    {
        logger.LogInformation("Revoking QR code with hash: {QrHash}", qrHash);

        var error = await qrCodeService.RevokeQrCodeByHashAsync(qrHash, request?.Reason, User);
        if (error != null)
        {
            logger.LogWarning("Failed to revoke QR code with hash {QrHash}: {Error}", qrHash, error);
            return BadRequest(new { message = error });
        }

        logger.LogInformation("Successfully revoked QR code with hash: {QrHash}", qrHash);
        return NoContent();
    }

    /// <summary>
    /// Reactivates a previously revoked QR code by ID.
    /// </summary>
    /// <param name="id">The QR code ID to reactivate.</param>
    /// <returns>NoContent if successful, error otherwise.</returns>
    [HttpPatch("{id}/reactivate")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult> ReactivateQrCode(int id)
    {
        logger.LogInformation("Reactivating QR code with ID: {QrCodeId}", id);

        var error = await qrCodeService.ReactivateQrCodeAsync(id, User);
        if (error != null)
        {
            logger.LogWarning("Failed to reactivate QR code with ID {QrCodeId}: {Error}", id, error);
            return BadRequest(new { message = error });
        }

        logger.LogInformation("Successfully reactivated QR code with ID: {QrCodeId}", id);
        return NoContent();
    }

    /// <summary>
    /// Reactivates a previously revoked QR code by hash.
    /// </summary>
    /// <param name="qrHash">The QR code hash to reactivate.</param>
    /// <returns>NoContent if successful, error otherwise.</returns>
    [HttpPatch("hash/{qrHash}/reactivate")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult> ReactivateQrCodeByHash(string qrHash)
    {
        logger.LogInformation("Reactivating QR code with hash: {QrHash}", qrHash);

        var error = await qrCodeService.ReactivateQrCodeByHashAsync(qrHash, User);
        if (error != null)
        {
            logger.LogWarning("Failed to reactivate QR code with hash {QrHash}: {Error}", qrHash, error);
            return BadRequest(new { message = error });
        }

        logger.LogInformation("Successfully reactivated QR code with hash: {QrHash}", qrHash);
        return NoContent();
    }

    /// <summary>
    /// Gets a QR code by its ID.
    /// </summary>
    /// <param name="id">The QR code ID.</param>
    /// <returns>QR code details if found.</returns>
    [HttpGet("{id}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult> GetQrCodeById(int id)
    {
        logger.LogInformation("Retrieving QR code with ID: {QrCodeId}", id);

        var qrCode = await qrCodeService.GetQrCodeByIdAsync(id);

        if (qrCode == null)
        {
            logger.LogWarning("QR code with ID {QrCodeId} not found", id);
            return NotFound(new { message = $"QR code with ID {id} not found" });
        }

        logger.LogInformation("Successfully retrieved QR code with ID: {QrCodeId}", id);
        return Ok(qrCode);
        // No try-catch - global handler will catch any unexpected errors
    }

    /// <summary>
    /// Gets a QR code image by its ID.
    /// Regenerates the QR code image from the stored hash.
    /// </summary>
    /// <param name="id">The QR code ID.</param>
    /// <returns>QR code image as PNG file.</returns>
    [HttpGet("{id}/image")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<IActionResult> GetQrCodeImage(int id)
    {
        logger.LogInformation("Retrieving QR code image for ID: {QrCodeId}", id);

        var qrCode = await qrCodeService.GetQrCodeByIdAsync(id);

        if (qrCode == null)
        {
            logger.LogWarning("QR code with ID {QrCodeId} not found", id);
            return NotFound(new { message = $"QR code with ID {id} not found" });
        }

        // Regenerate QR image from hash (same logic as generate endpoint)
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrCode.QrHash, QRCodeGenerator.ECCLevel.Q);
        var qrCodeImage = new PngByteQRCode(qrCodeData);

        // Get PNG as byte array
        byte[] imageBytes = qrCodeImage.GetGraphic(20);

        logger.LogInformation("Successfully generated image for QR code ID: {QrCodeId}", id);
        return File(imageBytes, "image/png");
    }

    /// <summary>
    /// Gets a QR code by its hash.
    /// </summary>
    /// <param name="qrHash">The QR code hash.</param>
    /// <returns>QR code details if found.</returns>
    [HttpGet("hash/{qrHash}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult> GetQrCodeByHash(string qrHash)
    {
        logger.LogInformation("Retrieving QR code with hash: {QrHash}", qrHash);

        var qrCode = await qrCodeService.GetQrCodeByHashAsync(qrHash);

        if (qrCode == null)
        {
            logger.LogWarning("QR code with hash {QrHash} not found", qrHash);
            return NotFound(new { message = $"QR code with hash {qrHash} not found" });
        }

        logger.LogInformation("Successfully retrieved QR code with hash: {QrHash}", qrHash);
        return Ok(qrCode);
        // No try-catch - global handler will catch any unexpected errors
    }

    /// <summary>
    /// Gets all QR codes for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>List of QR codes for the session.</returns>
    [HttpGet("session/{sessionId}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult> GetQrCodesBySessionId(int sessionId)
    {
        logger.LogInformation("Retrieving QR codes for session ID: {SessionId}", sessionId);

        try
        {
            // Get instructor ID for authorization check
            var instructorId = await userContextService.GetInstructorIdAsync(User);
            if (!instructorId.HasValue)
            {
                logger.LogWarning("Non-instructor user attempted to retrieve QR codes for session {SessionId}", sessionId);
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "User is not an instructor", errorCode = "NOT_INSTRUCTOR" });
            }

            // Get user role for authorization
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Unknown";

            // Verify instructor has access to this session (unless Admin)
            if (userRole == RoleConstants.Instructor)
            {
                var session = await sessionRepository.GetSessionByIdAsync(sessionId);

                if (session == null)
                {
                    logger.LogWarning("Session {SessionId} not found", sessionId);
                    return NotFound(new { message = $"Session {sessionId} not found", errorCode = "SESSION_NOT_FOUND" });
                }

                if (session.Schedule?.InstructorId != instructorId.Value)
                {
                    logger.LogWarning(
                        "Instructor {InstructorId} attempted unauthorized access to QR codes for session {SessionId} owned by instructor {OwnerId}",
                        instructorId.Value, sessionId, session.Schedule?.InstructorId);
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { message = "You do not have permission to view QR codes for this session", errorCode = "FORBIDDEN" });
                }
            }

            var qrCodes = await qrCodeService.GetQrCodesBySessionIdAsync(sessionId);

            var qrCodesList = qrCodes.ToList();
            if (!qrCodesList.Any())
            {
                logger.LogWarning("No QR codes found for session ID {SessionId}", sessionId);
                return NotFound(new { message = $"No QR codes found for session {sessionId}", errorCode = "NO_QRCODES_FOUND" });
            }

            logger.LogInformation("Successfully retrieved {Count} QR codes for session ID: {SessionId}",
                qrCodesList.Count, sessionId);
            return Ok(qrCodesList);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving QR codes for session {SessionId}", sessionId);
            return StatusCode(500, new { message = "An error occurred while retrieving QR codes", errorCode = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// Validates a QR code without recording attendance.
    /// </summary>
    /// <param name="qrHash">The QR code hash to validate.</param>
    /// <returns>Validation response with QR code details.</returns>
    [HttpGet("validate/{qrHash}")]
    [AllowAnonymous]
    public async Task<ActionResult> ValidateQrCode(string qrHash)
    {
        logger.LogInformation("Validating QR code with hash: {QrHash}", qrHash);

        var result = await qrCodeService.ValidateQrCodeAsync(qrHash);

        if (!result.IsValid)
        {
            logger.LogWarning("QR code validation failed: {Message}", result.Message);
            return Ok(result); // Return 200 with isValid: false
        }

        logger.LogInformation("Successfully validated QR code with hash: {QrHash}", qrHash);
        return Ok(result);
        // No try-catch - global handler will catch any unexpected errors
    }

    /// <summary>
    /// Scans a QR code and records attendance for a student.
    /// </summary>
    /// <param name="request">The scan request containing QR hash and student ID.</param>
    /// <returns>Scan response with attendance status.</returns>
    [HttpPost("scan")]
    [Authorize]
    public async Task<ActionResult> ScanQrCode([FromBody] ValidateQrCode request)
    {
        logger.LogInformation("Scanning QR code with hash: {QrHash} for student ID: {StudentId}",
            request.QrHash, request.StudentId);

        var result = await qrCodeService.ScanQrCodeAsync(request, User);

        if (!result.Success)
        {
            logger.LogWarning("QR code scan failed: {Message}", result.Message);
            return Ok(result); // Return 200 with success: false
        }

        logger.LogInformation("Successfully scanned QR code for student ID: {StudentId}", request.StudentId);
        return Ok(result);
        // No try-catch - global handler will catch any unexpected errors
    }

    /// <summary>
    /// Get scan history for a QR code by ID
    /// </summary>
    /// <param name="id">QR Code ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated scan history with statistics</returns>
    [HttpGet("{id}/scan-history")]
    [Authorize(Policy = "PrivilegedPolicy")]
    [ProducesResponseType(typeof(attendance_monitoring.Models.DTO.Response.QrCodeScanHistoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetScanHistory(
        int id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var instructorId = await userContextService.GetInstructorIdAsync(User);
            if (!instructorId.HasValue)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "User is not an instructor", errorCode = "NOT_INSTRUCTOR" });
            }

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Unknown";

            logger.LogInformation(
                "Instructor {InstructorId} ({UserRole}) requesting scan history for QR code {QrCodeId}",
                instructorId.Value, userRole, id);

            var result = await qrCodeService.GetScanHistoryAsync(
                id, instructorId.Value, userRole, pageNumber, pageSize);

            return Ok(result);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning("QR code not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message, errorCode = "QRCODE_NOT_FOUND" });
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning("Unauthorized access attempt: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You do not have permission to view this scan history", errorCode = "FORBIDDEN" });
        }
        catch (Exceptions.ValidationException ex)
        {
            logger.LogWarning("Validation error: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message, errorCode = "VALIDATION_ERROR" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving scan history for QR code {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving scan history" });
        }
    }

    /// <summary>
    /// Get scan history for a QR code by hash
    /// </summary>
    /// <param name="qrHash">QR Code hash</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Paginated scan history with statistics</returns>
    [HttpGet("hash/{qrHash}/scan-history")]
    [Authorize(Policy = "PrivilegedPolicy")]
    [ProducesResponseType(typeof(attendance_monitoring.Models.DTO.Response.QrCodeScanHistoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetScanHistoryByHash(
        string qrHash,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var instructorId = await userContextService.GetInstructorIdAsync(User);
            if (!instructorId.HasValue)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "User is not an instructor", errorCode = "NOT_INSTRUCTOR" });
            }

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Unknown";

            logger.LogInformation(
                "Instructor {InstructorId} ({UserRole}) requesting scan history for QR hash {QrHash}",
                instructorId.Value, userRole, qrHash);

            var result = await qrCodeService.GetScanHistoryByHashAsync(
                qrHash, instructorId.Value, userRole, pageNumber, pageSize);

            return Ok(result);
        }
        catch (EntityNotFoundException<string> ex)
        {
            logger.LogWarning("QR code not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message, errorCode = "QRCODE_NOT_FOUND" });
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning("Unauthorized access attempt: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You do not have permission to view this scan history", errorCode = "FORBIDDEN" });
        }
        catch (Exceptions.ValidationException ex)
        {
            logger.LogWarning("Validation error: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message, errorCode = "VALIDATION_ERROR" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving scan history for QR hash {Hash}", qrHash);
            return StatusCode(500, new { message = "An error occurred while retrieving scan history" });
        }
    }
}
