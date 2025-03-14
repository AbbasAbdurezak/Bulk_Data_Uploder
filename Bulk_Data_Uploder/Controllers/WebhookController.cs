using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class WebhookController : ControllerBase
{
    [HttpPost("notify")]
    public IActionResult Notify([FromBody] object payload)
    {
        // Handle webhook notification
        return Ok();
    }
}