namespace Bot.Core;

using Telegram.Bot;
using Telegram.Bot.Types;

public interface IPreHandler
{
    public Task<bool> CanContinue(Update updates, ITelegramBotClient botClient);
}
