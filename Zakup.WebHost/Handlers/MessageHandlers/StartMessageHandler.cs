using Bot.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.WebHost.Extensions;

namespace Zakup.WebHost.Handlers.MessageHandlers;

public class StartMessageHandler : IUpdatesHandler
{
    public bool ShouldHandle(Update update)
    {
        return update.IsNotEmptyMessage() && update.Message!.Text!.StartsWith("/start");
    }

    public Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}