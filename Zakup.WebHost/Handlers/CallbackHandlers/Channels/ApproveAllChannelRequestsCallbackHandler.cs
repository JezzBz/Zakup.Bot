using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.ApproveAllChannelRequest)]
public class ApproveAllChannelRequestsCallbackHandler : ICallbackHandler<ApproveAllChannelRequestsCallbackData>
{
    public Task Handle(ITelegramBotClient botClient, ApproveAllChannelRequestsCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ApproveAllChannelRequestsCallbackData Parse(List<string> parameters)
    {
        return new ApproveAllChannelRequestsCallbackData
        {
            ChannelId = long.Parse(parameters[0])
        };
    }
}