using attendance_monitoring.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace attendance_monitoring.Controllers;

[ApiController]
[Route("api")]
public class LoginController : Controller
{
    // GET
    private readonly ILoginRepository login;

    public LoginController(ILoginRepository login)
    {
        this.login = login;
    }
    
    
    [HttpGet]
    [Route("login")]
    public async Task<IActionResult> GetLogin()
    {
        var body = await login.GetLogin();
        return Ok();
    }

    [HttpPut]
    [Route("login{id}")]
    public async Task<IActionResult> UpdateLogin()
    {
        return Ok();
    }


    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> PostLogin()
    {
        return Ok();
    }

    [HttpDelete]
    [Route("login{id}")]
    public async Task<IActionResult> DeleteLogin()
    {
        return Ok();
    }
}