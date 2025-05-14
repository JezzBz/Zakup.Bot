namespace Bot.Core;

using Telegram.Bot;
using Telegram.Bot.Types;

public interface IUpdatesHandler
{
    static abstract bool ShouldHandle(Update update);

    Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
}
