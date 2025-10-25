using System.Text.Json;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace attendance_monitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrCodeController(
    IQrCodeService qrCodeService,
    ILogger<QrCodeController> logger) : ControllerBase
{
    /// <summary>
    /// Generates a new QR code, saves it to database, and returns the PNG image.
    /// </summary>
    /// <param name="request">QR code generation parameters.</param>
    /// <returns>PNG image of the QR code.</returns>
    [HttpPost("generate")]
    [Authorize(Policy = "PrivilegedPolicy")]
    public async Task<ActionResult> GenerateQrCode([FromBody] QrCodeRequest request)
    {
        logger.LogInformation("Generating QR code for schedule ID: {ScheduleId}, section ID: {SectionId}", 
            request.ScheduleId, request.SectionId);
        
        try
        {
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
            
            // Return image with proper content type
            return File(qrCodeImage, "image/png");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while generating QR code for section ID: {SectionId}", 
                request.SectionId);
            return Problem(
                detail: "An unexpected error occurred while generating the QR code",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
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

        try
        {
            var qrCode = await qrCodeService.GetQrCodeByIdAsync(id);

            if (qrCode == null)
            {
                logger.LogWarning("QR code with ID {QrCodeId} not found", id);
                return NotFound(new { message = $"QR code with ID {id} not found" });
            }

            logger.LogInformation("Successfully retrieved QR code with ID: {QrCodeId}", id);
            return Ok(qrCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while retrieving QR code with ID: {QrCodeId}", id);
            return Problem(
                detail: "An unexpected error occurred while retrieving the QR code",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
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

        try
        {
            var qrCode = await qrCodeService.GetQrCodeByHashAsync(qrHash);

            if (qrCode == null)
            {
                logger.LogWarning("QR code with hash {QrHash} not found", qrHash);
                return NotFound(new { message = $"QR code with hash {qrHash} not found" });
            }

            logger.LogInformation("Successfully retrieved QR code with hash: {QrHash}", qrHash);
            return Ok(qrCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while retrieving QR code with hash: {QrHash}", qrHash);
            return Problem(
                detail: "An unexpected error occurred while retrieving the QR code",
                statusCode: 500,
                title: "Internal Server Error"
            );
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

        try
        {
            var result = await qrCodeService.ValidateQrCodeAsync(qrHash);

            if (!result.IsValid)
            {
                logger.LogWarning("QR code validation failed: {Message}", result.Message);
                return Ok(result); // Return 200 with isValid: false
            }

            logger.LogInformation("Successfully validated QR code with hash: {QrHash}", qrHash);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while validating QR code with hash: {QrHash}", qrHash);
            return Problem(
                detail: "An unexpected error occurred while validating the QR code",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
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

        try
        {
            var result = await qrCodeService.ScanQrCodeAsync(request, User);

            if (!result.Success)
            {
                logger.LogWarning("QR code scan failed: {Message}", result.Message);
                return Ok(result); // Return 200 with success: false
            }

            logger.LogInformation("Successfully scanned QR code for student ID: {StudentId}", request.StudentId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while scanning QR code for student ID: {StudentId}",
                request.StudentId);
            return Problem(
                detail: "An unexpected error occurred while scanning the QR code",
                statusCode: 500,
                title: "Internal Server Error"
            );
        }
    }
}
