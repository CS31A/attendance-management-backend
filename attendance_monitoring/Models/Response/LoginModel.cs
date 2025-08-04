namespace attendance_monitoring.Models.Response;

public class LoginModel
{
    public int ID { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
}

public class UserModel
{
    public string Username { get; set; }
    public string Password { get; set; }
}