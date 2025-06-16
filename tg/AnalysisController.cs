using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
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
        _outputDirectory = configuration["OutputDirectory"] ?? "Output";
        
        // Ensure output directory exists
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    [HttpPost("complete")]
    public async Task<IActionResult> AnalysisComplete()
    {
        try
        {
            // Читаем тело запроса
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();
            
            // Десериализуем JSON
            var notification = JsonSerializer.Deserialize<AnalysisCompleteNotification>(json);
            
            if (notification == null || string.IsNullOrEmpty(notification.Guid) || string.IsNullOrEmpty(notification.FileUrl))
            {
                _logger.LogError("Invalid notification: missing required fields");
                return BadRequest("Invalid notification: missing required fields");
            }

            _logger.LogInformation($"Received completion notification for analysis {notification.Guid}");
            _logger.LogInformation($"File URL: {notification.FileUrl}");

            // Здесь можно добавить дополнительную логику обработки завершенного анализа
            // Например, обновление статуса в базе данных, отправка уведомлений и т.д.

            return Ok(new { message = "Notification received successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing completion notification");
            return StatusCode(500, "Internal server error while processing notification");
        }
    }

    [HttpPost("stream")]
    public async Task<IActionResult> StreamFile()
    {
        try
        {
            // Get GUID from header
            if (!Request.Headers.TryGetValue("X-GUID", out var guid))
            {
                return BadRequest("X-GUID header is required");
            }

            // Validate content type
            if (Request.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                return BadRequest("Invalid content type. Expected Excel file.");
            }

            // Create output file path
            var outputPath = Path.Combine(_outputDirectory, $"{guid}.xlsx");

            // Stream the file to disk
            using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                await Request.Body.CopyToAsync(fileStream);
            }

            _logger.LogInformation($"Successfully saved file for GUID: {guid} to {outputPath}");
            return Ok(new { message = "File received successfully", path = outputPath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file stream");
            return StatusCode(500, "Internal server error while processing file");
        }
    }
}

public class AnalysisCompleteNotification
{
    public string Guid { get; set; }
    public string FileUrl { get; set; }
} 