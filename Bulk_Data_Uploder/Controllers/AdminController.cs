using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok("Admin status check passed");
    }
}