using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Admin;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Admin;

[CallbackType(CallbackType.AdminUnmute)]
public class AdminUnmuteCallbackHandler : ICallbackHandler<AdminUnmuteCallbackData>
{
    private readonly UserService _userService;

    public AdminUnmuteCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, AdminUnmuteCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
       
        var msg = await botClient.SendTextMessageAsync(
            callbackQuery.Message.Chat.Id,
            "Пришлите id пользователя или перешлите сообщение от него, чтобы снять мут.",
            cancellationToken: cancellationToken
        );
        var state = new TelegramUserState
        {
            UserId = callbackQuery.From.Id,
            State = UserStateType.AdminUnmute,
            PreviousMessageId = msg.MessageId
        };
        await _userService.SetUserState(callbackQuery.From.Id, state, cancellationToken);
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
    }
} 