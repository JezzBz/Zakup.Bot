using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Common.Models;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.ChangeCannelLabel)]
public class ChangeChannelLabelCallbackHandler : ICallbackHandler<ChangeChannelLabelCallbackData>
{
    private readonly UserService _userService;

    public ChangeChannelLabelCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, ChangeChannelLabelCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userState = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        userState.State = UserStateType.CreateChannelAlias;
        userState.CachedValue = CacheHelper.ToCache(new CreateChannelCacheData
        {
            ChannelId = data.ChannelId,
            RequestFirstPost = false
        });
        await _userService.SetUserState(userState, cancellationToken);
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId, MessageTemplate.WriteNewLabel, cancellationToken: cancellationToken);
    }

    public ChangeChannelLabelCallbackData Parse(List<string> parameters)
    {
        return new ChangeChannelLabelCallbackData
        {
            ChannelId = long.Parse(parameters[0])
        };
    }
}