using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Admin;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Admin;

[CallbackType(CallbackType.AdminUnscam)]
public class AdminUnscamCallbackHandler : ICallbackHandler<AdminUnscamCallbackData>
{
    private readonly UserService _userService;

    public AdminUnscamCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, AdminUnscamCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var state = new TelegramUserState
        {
            UserId = callbackQuery.From.Id,
            State = UserStateType.AdminUnscam
        };
        await _userService.SetUserState(callbackQuery.From.Id, state, cancellationToken);
        await botClient.SendTextMessageAsync(
            callbackQuery.Message.Chat.Id,
            "Перешлите сообщение из канала или отправьте его id, чтобы снять отметку скама.",
            cancellationToken: cancellationToken
        );
    }
} 