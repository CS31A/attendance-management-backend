using System.Text.Json;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace attendance_monitoring.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrCodeController(ILogger<QrCodeController> logger) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<ActionResult> GenerateQrCode([FromBody] QrCodeRequest request)
    {
        logger.LogInformation("Generating QR code for section ID: {SectionId}", request.SectionId);
        
        try
        {
            // Generate unique key if not provided
            if (string.IsNullOrEmpty(request.UniqueKey))
            {
                request.UniqueKey = Guid.NewGuid().ToString();
            }

            // Serialize the request data to JSON
            var qrData = JsonSerializer.Serialize(new
            {
                sectionId = request.SectionId,
                schedule = request.Schedule,
                roomId = request.RoomId,
                subjectName = request.SubjectName,
                uniqueKey = request.UniqueKey,
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
}