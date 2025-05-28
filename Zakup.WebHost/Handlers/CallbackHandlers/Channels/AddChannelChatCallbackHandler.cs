using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.AddChannelChat)]
public class AddChannelChatCallbackHandler : ICallbackHandler<AddChannelChatCallbackData>
{
    public Task Handle(ITelegramBotClient botClient, AddChannelChatCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public AddChannelChatCallbackData Parse(List<string> parameters)
    {
        return new AddChannelChatCallbackData
        {
            ChannelId = long.Parse(parameters[0])
        };
    }

}