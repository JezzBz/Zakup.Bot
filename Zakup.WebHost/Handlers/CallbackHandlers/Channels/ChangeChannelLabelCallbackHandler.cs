using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.ChangeCannelLabel)]
public class ChangeChannelLabelCallbackHandler : ICallbackHandler<ChangeChannelLabelCallbackData>
{
    public Task Handle(ITelegramBotClient botClient, ChangeChannelLabelCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ChangeChannelLabelCallbackData Parse(List<string> parameters)
    {
        return new ChangeChannelLabelCallbackData
        {
            ChannelId = long.Parse(parameters[0])
        };
    }
}