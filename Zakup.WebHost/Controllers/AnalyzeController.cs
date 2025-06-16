using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly ILogger<AnalysisController> _logger;
    private readonly string _outputDirectory;

    public AnalysisController(ILogger<AnalysisController> logger, IConfiguration configuration)
    {
        _logger = logger;
        
    }

    [HttpPost("complete")]
    public async Task<IActionResult> AnalysisComplete(AnalysisCompleteNotification result)
    {
        var a = result;
        
        return Ok(a);
    }
    
}

public class AnalysisCompleteNotification
{
    public string Guid { get; set; }
    public string? FileUrl { get; set; }
    
}