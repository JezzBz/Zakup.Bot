using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.AddChannelChat)]
public class AddChannelChatCallbackHandler : ICallbackHandler<AddChannelChatCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly UserService _userService;
    public AddChannelChatCallbackHandler(ChannelService channelService, UserService userService)
    {
        _channelService = channelService;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, AddChannelChatCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var channel = await _channelService.GetChannel(data.ChannelId, cancellationToken);
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

        if (channel?.ChannelChatId is not null)
        {
            return;
        }
        
        var message = await botClient.SafeEdit(
            callbackQuery.From.Id,
            callbackQuery.Message.MessageId,
            MessageTemplate.AddChannelChatText, cancellationToken: cancellationToken);

        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        state!.State = UserStateType.AddChannelChat;
        state.PreviousMessageId = message.MessageId;
        state.CachedValue = CacheHelper.ToCache(new ChannelIdCache()
        {
            ChannelId = data.ChannelId
        });
        
        await _userService.SetUserState(state, cancellationToken);
    }
}