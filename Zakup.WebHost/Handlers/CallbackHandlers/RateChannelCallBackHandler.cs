using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Common.Models;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

/// <summary>
/// Обработчик команды "Оценка канала"
/// </summary>
[CallbackType(CallbackType.RateChannel)]
public class RateChannelCallBackHandler : ICallbackHandler<RateChannelCallbackData>
{
    public async Task Handle(ITelegramBotClient botClient, RateChannelCallbackData data, CallbackQuery callbackQuery)
    {
        
    }
    
    public RateChannelCallbackData Parse(List<string> parameters)
    {
        return new RateChannelCallbackData
        {
            RateType = Enum.Parse<ChannelRateType>(parameters[0]),
            ChannelId = long.Parse(parameters[1])
        };
    }
}