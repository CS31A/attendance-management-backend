using System.Security.Claims;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

/// <summary>
/// Service interface for managing fingerprint biometric authentication and attendance.
/// </summary>
public interface IFingerprintService
{
    #region Registration Operations

    /// <summary>
    /// Removes (soft deletes) a fingerprint registration.
    /// </summary>
    /// <param name="fingerprintId">The fingerprint ID to remove.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>A response with the removal result.</returns>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when fingerprint is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task<FingerprintRegistrationResponseDto> RemoveFingerprintAsync(int fingerprintId, ClaimsPrincipal user);

    /// <summary>
    /// Starts a backend-driven fingerprint enrollment session for a device.
    /// </summary>
    /// <param name="request">The enrollment session request.</param>
    /// <param name="user">The authenticated admin or instructor starting the session.</param>
    /// <returns>The created enrollment session metadata.</returns>
    Task<FingerprintEnrollmentSessionResponseDto> StartEnrollmentSessionAsync(
        StartFingerprintEnrollmentSessionRequest request,
        ClaimsPrincipal user);

    /// <summary>
    /// Gets the next pending enrollment session for a device.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="apiKey">The device API key.</param>
    /// <returns>The pending enrollment session if one exists; otherwise null.</returns>
    Task<FingerprintEnrollmentSessionResponseDto?> GetPendingEnrollmentSessionAsync(string deviceId, string apiKey);

    /// <summary>
    /// Completes an enrollment session after device-side enrollment succeeds or fails.
    /// </summary>
    /// <param name="request">The device completion payload.</param>
    /// <param name="apiKey">The device API key.</param>
    /// <returns>A registration response describing the final state.</returns>
    Task<FingerprintRegistrationResponseDto> CompleteEnrollmentSessionAsync(
        CompleteFingerprintEnrollmentRequest request,
        string apiKey);

    #endregion

    #region Scan/Attendance Operations

    /// <summary>
    /// Scans a fingerprint by device identifier and sensor slot, then records attendance.
    /// </summary>
    /// <param name="request">The sensor-slot scan request.</param>
    /// <param name="apiKey">The device API key.</param>
    /// <returns>A scan response with attendance status and context information.</returns>
    Task<FingerprintScanResponseDto> ScanFingerprintBySensorAsync(ScanFingerprintBySensorRequest request, string apiKey);

    #endregion

    #region Query Operations

    /// <summary>
    /// Gets fingerprint information for a student.
    /// </summary>
    /// <param name="studentId">The student ID.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>The fingerprint response DTO if found.</returns>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when fingerprint is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task<FingerprintResponseDto> GetFingerprintByStudentIdAsync(int studentId, ClaimsPrincipal user);

    /// <summary>
    /// Gets all fingerprints registered for a specific device.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>A collection of fingerprint response DTOs.</returns>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task<IEnumerable<FingerprintResponseDto>> GetFingerprintsByDeviceIdAsync(string deviceId, ClaimsPrincipal user);

    /// <summary>
    /// Gets all active fingerprints.
    /// </summary>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>A collection of fingerprint response DTOs.</returns>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task<IEnumerable<FingerprintResponseDto>> GetAllActiveFingerprintsAsync(ClaimsPrincipal user);

    /// <summary>
    /// Checks if a student has a registered fingerprint.
    /// </summary>
    /// <param name="studentId">The student ID.</param>
    /// <returns>True if the student has a registered fingerprint; otherwise, false.</returns>
    Task<bool> StudentHasFingerprintAsync(int studentId);

    #endregion
}
