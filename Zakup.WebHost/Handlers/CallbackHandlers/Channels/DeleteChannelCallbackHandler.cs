using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.DeleteChannel)]
public class DeleteChannelCallbackHandler : ICallbackHandler<DeleteChannelCallbackData>
{
    public Task Handle(ITelegramBotClient botClient, DeleteChannelCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public DeleteChannelCallbackData Parse(List<string> parameters)
    {
        return new DeleteChannelCallbackData
        {
            ChannelId = long.Parse(parameters[0])
        };
    }
}