using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Admin;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Admin;

[CallbackType(CallbackType.AdminMute)]
public class AdminMuteCallbackHandler : ICallbackHandler<AdminMuteCallbackData>
{
    private readonly UserService _userService;

    public AdminMuteCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, AdminMuteCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var state = new TelegramUserState
        {
            UserId = callbackQuery.From.Id,
            State = UserStateType.AdminMute
        };
        await _userService.SetUserState(callbackQuery.From.Id, state, cancellationToken);
        await botClient.SendTextMessageAsync(
            callbackQuery.Message.Chat.Id,
            "Пришлите id пользователя или перешлите сообщение от него, чтобы выдать мут.",
            cancellationToken: cancellationToken
        );
    }
} 