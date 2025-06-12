using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Admin;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Admin;

[CallbackType(CallbackType.AdminDelete)]
public class AdminDeleteCallbackHandler : ICallbackHandler<AdminDeleteCallbackData>
{
    private readonly UserService _userService;

    public AdminDeleteCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, AdminDeleteCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var state = new TelegramUserState
        {
            UserId = callbackQuery.From.Id,
            State = UserStateType.AdminDelete
        };
        await _userService.SetUserState(callbackQuery.From.Id, state, cancellationToken);
        await botClient.SendTextMessageAsync(
            callbackQuery.Message.Chat.Id,
            "Перешлите сообщение из канала или отправьте его id, чтобы удалить канал.",
            cancellationToken: cancellationToken
        );
    }
} 