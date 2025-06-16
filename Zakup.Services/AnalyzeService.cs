using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zakup.Entities;
using Zakup.EntityFramework;

public class AnalyzeService
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnalyzeService> _logger;
    private readonly string _apiBaseUrl;

    public AnalyzeService(
        ApplicationDbContext context,
        HttpClient httpClient,
        ILogger<AnalyzeService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
        _apiBaseUrl = configuration["AnalyzeApi:BaseUrl"] ?? "http://localhost:8000";
    }
    
    public async Task<long> GetAnalyzePoints(long userId, CancellationToken cancellationToken = default)
    {
        var balance =  await _context.AnalyzeBalances.FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken: cancellationToken);
        if (balance == null)
        {
            balance = await CreateBalance(userId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return balance.Balance;
    }
    
    public async Task<Guid> Analyze(long userId, string channel, CancellationToken cancellationToken = default)
    {
        var balance = await _context.AnalyzeBalances.FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken: cancellationToken);
        if (balance == null)
        {
            balance = await CreateBalance(userId, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (balance.Balance <= 0)
        {
            throw new InvalidOperationException($"UserId:{userId} баланс поиска закончился");
        }

        var analyze = new ChannelsAnalyzeProcess
        {
            Id = Guid.NewGuid(),
            AnalyzeTarget = channel,
            UserId = userId,
        };
        
        balance.Balance--;
        _context.AnalyzeBalances.Update(balance);
        await _context.AnalyzeProcesses.AddAsync(analyze, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        try
        {
            await StartAnalysis(analyze.Id, channel, cancellationToken);
            return analyze.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске анализа для канала {Channel}", channel);
            throw;
        }
    }

    private async Task StartAnalysis(Guid taskId, string channel, CancellationToken cancellationToken)
    {
        var request = new AnalyzeRequest
        {
            TaskId = taskId.ToString(),
            Channel = channel,
            OutputFormat = "xlsx"
        };

        var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/analyze", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<AnalyzeResponse>(cancellationToken: cancellationToken);
        if (result?.Status != "processing")
        {
            throw new InvalidOperationException($"Не удалось запустить анализ. Статус: {result?.Status}");
        }
    }

    public async Task<AnalyzeResult> GetAnalysisResult(Guid taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/result/{taskId}", cancellationToken);
            
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new AnalyzeResult { Status = "not_found" };
            }

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                return new AnalyzeResult { Status = "processing" };
            }

            response.EnsureSuccessStatusCode();

            // Если получили файл
            if (response.Content.Headers.ContentType?.MediaType?.Contains("application/") == true)
            {
                var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') 
                               ?? $"analysis_{taskId}.xlsx";
                
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                
                return new AnalyzeResult
                {
                    Status = "completed",
                    FileStream = stream,
                    FileName = fileName
                };
            }

            // Если получили JSON с ошибкой
            var errorResult = await response.Content.ReadFromJsonAsync<AnalyzeResult>(cancellationToken: cancellationToken);
            return errorResult ?? new AnalyzeResult { Status = "error", Error = "Unknown error" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении результата анализа для задачи {TaskId}", taskId);
            return new AnalyzeResult { Status = "error", Error = ex.Message };
        }
    }
    
    private async Task<AnalyzePointsBalance> CreateBalance(long userId, CancellationToken cancellationToken = default)
    {
        var balance = new AnalyzePointsBalance
        {
            UserId = userId,
            Balance = 0
        };
        
        return (await _context.AddAsync(balance, cancellationToken)).Entity;
    }


    public async Task<bool> IsAnalysisComplete(Guid taskId, CancellationToken cancellationToken = default)
    {
        var result = await GetAnalysisResult(taskId, cancellationToken);
        return result.Status == "completed";
    }
    
    public class AnalyzeRequest
    {
        public string TaskId { get; set; }
        public string Channel { get; set; }
        public string OutputFormat { get; set; } = "xlsx";
    }

    public class AnalyzeResponse
    {
        public string TaskId { get; set; }
        public string Status { get; set; }
    }

    public class AnalyzeResult
    {
        public string Status { get; set; }
        public Stream FileStream { get; set; }
        public string FileName { get; set; }
        public string Error { get; set; }
    }
}