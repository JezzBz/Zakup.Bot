using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.WebHost.Constants;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly ILogger<AnalysisController> _logger;
    private readonly AnalyzeService _analyzeService;
    private readonly ITelegramBotClient _botClient;
    private readonly HttpClient _httpClient;

    public AnalysisController(ILogger<AnalysisController> logger, IConfiguration configuration, AnalyzeService analyzeService, ITelegramBotClient botClient, HttpClient httpClient)
    {
        _logger = logger;
        _analyzeService = analyzeService;
        _botClient = botClient;
        _httpClient = httpClient;
    }

    [HttpPost("complete")]
    public async Task AnalysisComplete(AnalysisCompleteNotification result)
    {
        var id = Guid.Parse(result.Guid);
        
        var process = await _analyzeService.Complete(id, result.FileUrl == null);
        if (result.FileUrl == null)
        {
            await _botClient.SendTextMessageAsync(process.UserId, MessageTemplate.AnalyzeError);
            return;
        }
        var response = await _httpClient.GetAsync(result.FileUrl);
        await _botClient.SendDocumentAsync(process.UserId,
            InputFile.FromStream(await response.Content.ReadAsStreamAsync(),$"{id}.xlsx"),
            caption: MessageTemplate.AnalyzeSuccess(id));
    }
    
}

public class AnalysisCompleteNotification
{
    public string Guid { get; set; }
    public string? FileUrl { get; set; }
    
}