using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.ChannelAutoApprove)]
public class ChannelAutoApproveCallbackHandler : ICallbackHandler<ChannelAutoApproveCallbackData>
{
    public Task Handle(ITelegramBotClient botClient, ChannelAutoApproveCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ChannelAutoApproveCallbackData Parse(List<string> parameters)
    {
        return new ChannelAutoApproveCallbackData()
        {
            ChannelId = long.Parse(parameters[0])
        };
    }
}