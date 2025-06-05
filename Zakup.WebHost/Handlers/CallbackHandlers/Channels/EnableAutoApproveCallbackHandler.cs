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

[CallbackType(CallbackType.EnableAutoApprove)]
public class EnableAutoApproveCallbackHandler : ICallbackHandler<EnableAutoApproveCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly UserService _userService;

    public EnableAutoApproveCallbackHandler(ChannelService channelService, UserService userService)
    {
        _channelService = channelService;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, EnableAutoApproveCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var channel = await _channelService.GetChannel(data.ChannelId, cancellationToken);
        if (channel is null)
        {
            await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId, MessageTemplate.ChannelNotFound, cancellationToken: cancellationToken);
            return;
        }

        if (channel.MinutesToAcceptRequest.HasValue)
        {
            await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId,
                MessageTemplate.AutoApproveAcctuallyEnabled, cancellationToken: cancellationToken);
            return;
        }
        var userState = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        userState.Clear();
        userState.State = UserStateType.SetApproveTime;
        userState.CachedValue = CacheHelper.ToCache(new SetApproveTimeCache
        {
            ChannelId = data.ChannelId
        });
        
        await _userService.SetUserState(callbackQuery.From.Id, userState, cancellationToken);
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId,
            MessageTemplate.WriteAutoApproveMinutes, cancellationToken: cancellationToken);
    }
}