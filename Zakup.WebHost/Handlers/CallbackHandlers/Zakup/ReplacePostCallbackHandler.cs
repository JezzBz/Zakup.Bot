using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Zakup;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

public class ReplacePostCallbackHandler : ICallbackHandler<ReplacePostCallbackData>
{
    public Task Handle(ITelegramBotClient botClient, ReplacePostCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}