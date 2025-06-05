using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.CheckSubscribers)]
public class CheckSubscribersCallbackHandler : ICallbackHandler<CheckSubscribersCallbackData>
{
    private readonly UserService _userService;

    public CheckSubscribersCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, CheckSubscribersCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        state.State = UserStateType.PdpCheck;
        state.CachedValue = CacheHelper.ToCache(new ZakupIdCache
        {
            ZakupId = data.ZakupId
        });
        await _userService.SetUserState(state, cancellationToken);
        
        await botClient.SafeEdit(
            callbackQuery.From.Id,
            callbackQuery.Message!.MessageId,
            MessageTemplate.PdpCheckRequest, cancellationToken: cancellationToken);
    }
}