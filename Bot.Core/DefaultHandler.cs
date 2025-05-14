namespace Bot.Core;

using Telegram.Bot;
using Telegram.Bot.Types;

public class DefaultHandler : IUpdatesHandler
{
    public static bool ShouldHandle(Update update)
    {
        return true;
    }

    public async Task Handle(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        /*for (int i = 0; i < 1000; i++)
        {
            await _.SendTextMessageAsync(update.Message.Chat.Id, i.ToString(), cancellationToken: cancellationToken);
        }*/
    }
}
