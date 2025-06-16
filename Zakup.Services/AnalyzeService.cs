using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zakup.Entities;
using Zakup.EntityFramework;
using Zakup.Services.Extensions;

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
        _apiBaseUrl = configuration["AnalyzeApi:BaseUrl"]!;
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

        await _context.ExecuteInTransactionAsync(async () =>
        {
            balance.Balance--;
            _context.AnalyzeBalances.Update(balance);
            await _context.AnalyzeProcesses.AddAsync(analyze, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        
            try
            {
                await StartAnalysis(analyze.Id, channel, cancellationToken);
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запуске анализа для канала {Channel}", channel);
                throw;
            }
        });
        
        return analyze.Id;
    }

    private async Task StartAnalysis(Guid taskId, string channel, CancellationToken cancellationToken)
    {
        var request = new AnalyzeRequest
        {
           guid = taskId.ToString(),
           channel_name = channel
        };

        var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/analyze", request, cancellationToken);
        response.EnsureSuccessStatusCode();
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
    
    
    public class AnalyzeRequest
    {
        public string guid { get; set; }
        public string channel_name { get; set; }
    }

    public class AnalyzeResponse
    {
        public string Guid { get; set; }
        public string Status { get; set; }
    }

    public async Task<ChannelsAnalyzeProcess> Complete(Guid id, bool result)
    {
        var process = await _context.AnalyzeProcesses.FirstOrDefaultAsync(q => q.Id == id);
        process.Success = result;
        _context.Update(process);
        
        if (!result)
        {
            var balance = await _context.AnalyzeBalances.FirstAsync(q => q.UserId == process.UserId);
            balance.Balance++;
            _context.AnalyzeBalances.Update(balance);
            await _context.SaveChangesAsync();
        }
        
        await _context.SaveChangesAsync();
        return process;
    }

    public async Task<long> UpdateBalance(long userId, long points, CancellationToken cancellationToken = default)
    {
        var balance = await _context.AnalyzeBalances.FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken: cancellationToken);
        if (balance == null)
        {
            balance = await CreateBalance(userId, cancellationToken);
            balance.Balance += points;
            await _context.SaveChangesAsync(cancellationToken);
            return points;
        }
        
        balance.Balance += points;
        await _context.SaveChangesAsync(cancellationToken);
        return balance.Balance;
    }
}