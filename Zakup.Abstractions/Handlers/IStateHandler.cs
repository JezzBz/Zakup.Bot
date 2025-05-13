using Telegram.Bot;
using Telegram.Bot.Types;

namespace Zakup.Abstractions.Handlers;

public interface IStateHandler
{
    Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken);
}