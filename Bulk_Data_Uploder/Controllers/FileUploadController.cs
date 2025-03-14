using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly FileParserService _fileParserService;
    private readonly AIIntegrationService _aiIntegrationService;

    public FileUploadController(FileParserService fileParserService, AIIntegrationService aiIntegrationService)
    {
        _fileParserService = fileParserService;
        _aiIntegrationService = aiIntegrationService;
    }

    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is empty");

        var records = await _fileParserService.ParseFileAsync(file);
        var processedRecords = await _aiIntegrationService.ProcessDataAsync(records);

        return Ok(processedRecords);
    }
}