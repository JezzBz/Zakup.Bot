using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.AddChannelAdmin)]
public class AddChannelAdminCallbackHandler : ICallbackHandler<AddChannelAdminCallbackData>
{
    public Task Handle(ITelegramBotClient botClient, AddChannelAdminCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public AddChannelAdminCallbackData Parse(List<string> parameters)
    {
        return new AddChannelAdminCallbackData
        {
            ChannelId = long.Parse(parameters[0])
        };
    }
}