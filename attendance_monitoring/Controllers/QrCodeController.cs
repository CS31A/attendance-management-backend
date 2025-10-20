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
    [HttpPost("generate")]
    public async Task<ActionResult> GenerateQrCode([FromBody] QrCodeRequest request)
    {
        logger.LogInformation("Generating QR code for section ID: {SectionId}", request.SectionId);
        
        try
        {
            // Serialize the request data to JSON
            var qrData = JsonSerializer.Serialize(new
            {
                scheduleId = request.ScheduleId,
                sectionId = request.SectionId,
                actualRoomId = request.ActualRoomId,
                expirationMinutes = request.ExpirationMinutes,
                maxUsage = request.MaxUsage,
                uniqueHash = request.UniqueHash,
                timestamp = DateTime.UtcNow
            });

            // Generate QR Code
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            
            // Get PNG as byte array
            byte[] qrCodeImage = qrCode.GetGraphic(20);

            logger.LogInformation("Successfully generated QR code for section ID: {SectionId}", request.SectionId);
            
            // Return image with proper content type
            return File(qrCodeImage, "image/png");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while generating QR code for section ID: {SectionId}", request.SectionId);
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
}
