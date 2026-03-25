using System.Security.Claims;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller for managing fingerprint biometric authentication and attendance.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FingerprintController(
    IFingerprintService fingerprintService,
    ILogger<FingerprintController> logger) : ControllerBase
{
    private const string DeviceApiKeyHeader = "X-Device-Api-Key";

    #region Registration Endpoints

    /// <summary>
    /// Removes (soft deletes) a fingerprint registration.
    /// Requires Admin or Instructor role.
    /// </summary>
    /// <param name="fingerprintId">The fingerprint ID to remove.</param>
    /// <returns>Removal result.</returns>
    /// <response code="200">Fingerprint removed successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User not authorized</response>
    /// <response code="404">Fingerprint not found</response>
    [HttpDelete("{fingerprintId}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    [ProducesResponseType(typeof(FingerprintRegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FingerprintRegistrationResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(FingerprintRegistrationResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(FingerprintRegistrationResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FingerprintRegistrationResponseDto>> RemoveFingerprint(int fingerprintId)
    {
        logger.LogInformation("Fingerprint removal request for ID: {FingerprintId}", fingerprintId);

        try
        {
            var response = await fingerprintService.RemoveFingerprintAsync(fingerprintId, User);
            logger.LogInformation("Fingerprint removed successfully for ID: {FingerprintId}", fingerprintId);
            return Ok(response);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Fingerprint not found for removal");
            return NotFound(new FingerprintRegistrationResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning(ex, "Unauthorized fingerprint removal attempt");
            return StatusCode(StatusCodes.Status403Forbidden, new FingerprintRegistrationResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Starts a backend-driven fingerprint enrollment session for a device.
    /// Requires Admin or Instructor role.
    /// </summary>
    [HttpPost("enrollment-sessions")]
    [Authorize(Policy = "PrivilegedPolicy")]
    [ProducesResponseType(typeof(FingerprintEnrollmentSessionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FingerprintEnrollmentSessionResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FingerprintEnrollmentSessionResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(FingerprintEnrollmentSessionResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FingerprintEnrollmentSessionResponseDto>> StartEnrollmentSession(
        [FromBody] StartFingerprintEnrollmentSessionRequest request)
    {
        logger.LogInformation(
            "Fingerprint enrollment session requested for StudentId: {StudentId}, DeviceId: {DeviceId}",
            request.StudentId,
            request.DeviceId);

        if (!ModelState.IsValid)
        {
            return BadRequest(new FingerprintEnrollmentSessionResponseDto
            {
                Success = false,
                Message = "Invalid request data"
            });
        }

        try
        {
            var response = await fingerprintService.StartEnrollmentSessionAsync(request, User);
            return Ok(response);
        }
        catch (EntityNotFoundException<int> ex)
        {
            return NotFound(new FingerprintEnrollmentSessionResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new FingerprintEnrollmentSessionResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (EntityUnauthorizedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new FingerprintEnrollmentSessionResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    #endregion

    #region Scan/Attendance Endpoints

    /// <summary>
    /// Gets the current pending enrollment session for a device.
    /// Called by the ESP32 device while polling for new enrollment work.
    /// </summary>
    [HttpGet("devices/{deviceId}/enrollment-session")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FingerprintEnrollmentSessionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FingerprintEnrollmentSessionResponseDto>> GetPendingEnrollmentSession(string deviceId)
    {
        try
        {
            var response = await fingerprintService.GetPendingEnrollmentSessionAsync(deviceId, GetDeviceApiKey());
            if (response == null)
            {
                return NoContent();
            }

            return Ok(response);
        }
        catch (EntityUnauthorizedException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Completes a pending enrollment session after the device finishes local enrollment.
    /// </summary>
    [HttpPost("devices/enrollment-result")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FingerprintRegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FingerprintRegistrationResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FingerprintRegistrationResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FingerprintRegistrationResponseDto>> CompleteEnrollmentSession(
        [FromBody] CompleteFingerprintEnrollmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new FingerprintRegistrationResponseDto
            {
                Success = false,
                Message = "Invalid request data"
            });
        }

        try
        {
            var response = await fingerprintService.CompleteEnrollmentSessionAsync(request, GetDeviceApiKey());
            return Ok(response);
        }
        catch (EntityUnauthorizedException ex)
        {
            return Unauthorized(new FingerprintRegistrationResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (EntityNotFoundException<string> ex)
        {
            return NotFound(new FingerprintRegistrationResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new FingerprintRegistrationResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Device-focused scan endpoint that matches by sensor slot instead of raw template data.
    /// </summary>
    [HttpPost("devices/scan")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FingerprintScanResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FingerprintScanResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(FingerprintScanResponseDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FingerprintScanResponseDto>> ScanFingerprintBySensor(
        [FromBody] ScanFingerprintBySensorRequest request)
    {
        logger.LogInformation(
            "Fingerprint sensor-slot scan request from device: {DeviceId}, Slot: {Slot}",
            request.DeviceId,
            request.SensorFingerprintId);

        if (!ModelState.IsValid)
        {
            return BadRequest(new FingerprintScanResponseDto
            {
                Success = false,
                Message = "Invalid request data"
            });
        }

        try
        {
            var response = await fingerprintService.ScanFingerprintBySensorAsync(request, GetDeviceApiKey());
            return Ok(response);
        }
        catch (EntityUnauthorizedException ex)
        {
            return Unauthorized(new FingerprintScanResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new FingerprintScanResponseDto
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    #endregion

    #region Query Endpoints

    /// <summary>
    /// Gets fingerprint information for a student.
    /// Students can only view their own fingerprint. Admin/Instructors can view any.
    /// </summary>
    /// <param name="studentId">The student ID.</param>
    /// <returns>Fingerprint information.</returns>
    /// <response code="200">Fingerprint retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User not authorized</response>
    /// <response code="404">Fingerprint not found</response>
    [HttpGet("student/{studentId}")]
    [Authorize]
    [ProducesResponseType(typeof(FingerprintResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FingerprintResponseDto>> GetFingerprintByStudentId(int studentId)
    {
        logger.LogInformation("Retrieving fingerprint for student ID: {StudentId}", studentId);

        try
        {
            var response = await fingerprintService.GetFingerprintByStudentIdAsync(studentId, User);
            return Ok(response);
        }
        catch (EntityNotFoundException<int> ex)
        {
            logger.LogWarning(ex, "Fingerprint not found for student {StudentId}", studentId);
            return NotFound(new { message = ex.Message });
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning(ex, "Unauthorized attempt to view fingerprint for student {StudentId}", studentId);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets all fingerprints registered for a specific device.
    /// Requires Admin or Instructor role.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <returns>List of fingerprints for the device.</returns>
    /// <response code="200">Fingerprints retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User not authorized</response>
    [HttpGet("device/{deviceId}")]
    [Authorize(Policy = "PrivilegedPolicy")]
    [ProducesResponseType(typeof(IEnumerable<FingerprintResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FingerprintResponseDto>>> GetFingerprintsByDeviceId(string deviceId)
    {
        logger.LogInformation("Retrieving fingerprints for device: {DeviceId}", deviceId);

        try
        {
            var response = await fingerprintService.GetFingerprintsByDeviceIdAsync(deviceId, User);
            return Ok(response);
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning(ex, "Unauthorized attempt to view device fingerprints");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets all active fingerprints in the system.
    /// Requires Admin or Instructor role.
    /// </summary>
    /// <returns>List of all active fingerprints.</returns>
    /// <response code="200">Fingerprints retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User not authorized</response>
    [HttpGet]
    [Authorize(Policy = "PrivilegedPolicy")]
    [ProducesResponseType(typeof(IEnumerable<FingerprintResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<FingerprintResponseDto>>> GetAllActiveFingerprints()
    {
        logger.LogInformation("Retrieving all active fingerprints");

        try
        {
            var response = await fingerprintService.GetAllActiveFingerprintsAsync(User);
            return Ok(response);
        }
        catch (EntityUnauthorizedException ex)
        {
            logger.LogWarning(ex, "Unauthorized attempt to view all fingerprints");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Checks if a student has a registered fingerprint.
    /// </summary>
    /// <param name="studentId">The student ID.</param>
    /// <returns>True if the student has a registered fingerprint.</returns>
    /// <response code="200">Check completed successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("check/{studentId}")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> CheckStudentHasFingerprint(int studentId)
    {
        logger.LogInformation("Checking if student {StudentId} has a fingerprint", studentId);

        var hasFingerprint = await fingerprintService.StudentHasFingerprintAsync(studentId);
        return Ok(new { studentId, hasFingerprint });
    }

    #endregion

    private string GetDeviceApiKey()
    {
        return Request.Headers.TryGetValue(DeviceApiKeyHeader, out var apiKey)
            ? apiKey.ToString()
            : string.Empty;
    }
}
