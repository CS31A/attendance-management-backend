namespace attendance_monitoring.IServices;

public interface ITokenValidationService
{
    Task<bool> IsTokenBlacklistedAsync(string jti);
}