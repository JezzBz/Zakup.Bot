namespace Bot.Core;

using Microsoft.Extensions.Logging;
using Telegram.Bot;

public class TelegramBotWorker : Worker
{
	private BotBuilder _builder;
	private ILogger<ITelegramBotClient>? _logger;
    private readonly CancellationTokenSource cts = new();

	public TelegramBotWorker(BotBuilder builder, ILogger<ITelegramBotClient> logger = null)
	{
		_builder = builder;
		_logger = logger;
	}
    

	public Task StopAsync(CancellationToken cancellationToken)
	{
		if (_logger is not null)
		{
			_logger.LogInformation("Telegram bot stop receiving updates");
		}
		
		return Task.CompletedTask;
	}

    public override ValueTask OnStart(CancellationToken ct = default)
    {
        var container = _builder.Build();
		
        if (_logger is not null)
        {
            _logger.LogInformation("Telegram bot started receiving updates");
        }

        container.Start(cts.Token);
        return ValueTask.CompletedTask;
    }

    public override async ValueTask OnUpdate(CancellationToken ct = default)
    {
        await Task.Delay(1000, ct);
    }

    public override ValueTask OnDestroy(CancellationToken ct = default)
    {
        if (_logger is not null)
        {
            _logger.LogInformation("Telegram bot stop receiving updates");
        }
        
        cts.Cancel();
        return ValueTask.CompletedTask;
    }
}
