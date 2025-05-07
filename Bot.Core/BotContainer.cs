namespace Bot.Core;

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

internal class BotContainer
{
    private readonly Func<ITelegramBotClient, Update, CancellationToken, Task> _updatesHandler;

    private readonly Func<ITelegramBotClient, Exception, CancellationToken, Task> _errorHandler;

    private readonly ITelegramBotClient _client;

    private readonly ReceiverOptions _receiverOptions;
	
    internal BotContainer(Func<ITelegramBotClient, Update, CancellationToken, Task> updatesHandler, Func<ITelegramBotClient, Exception, CancellationToken, Task> errorHandler, ITelegramBotClient client, ReceiverOptions receiverOptions)
    {
        _updatesHandler = updatesHandler;
        _errorHandler = errorHandler;
        _client = client;
        _receiverOptions = receiverOptions;
    }
	
    internal Task Start(CancellationToken cancellationToken) => _client.ReceiveAsync(_updatesHandler, _errorHandler, _receiverOptions ,cancellationToken: cancellationToken);
}
