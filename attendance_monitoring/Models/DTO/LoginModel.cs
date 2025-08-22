namespace attendance_monitoring.Models.DTO;

public class LoginModel
{
    public int ID { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
}