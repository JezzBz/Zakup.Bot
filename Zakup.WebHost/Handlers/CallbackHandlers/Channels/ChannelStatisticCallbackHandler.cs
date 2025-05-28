using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.ChannelStatistic)]
public class ChannelStatisticCallbackHandler : ICallbackHandler<ChannelStatisticCallbackData>
{
    public Task Handle(ITelegramBotClient botClient, ChannelStatisticCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ChannelStatisticCallbackData Parse(List<string> parameters)
    {
        return new ChannelStatisticCallbackData
        {
            ChannelId = long.Parse(parameters[0])
        };
    }
}