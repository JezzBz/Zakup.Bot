using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Admin;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Admin;

[CallbackType(CallbackType.AdminConfirmMute)]
public class AdminConfirmMuteCallbackHandler : ICallbackHandler<AdminConfirmMuteCallbackData>
{
    private readonly UserService _userService;
    private readonly MessagesService _messagesService;

    public AdminConfirmMuteCallbackHandler(UserService userService, MessagesService messagesService)
    {
        _userService = userService;
        _messagesService = messagesService;
    }

    public async Task Handle(ITelegramBotClient botClient, AdminConfirmMuteCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var adminUser = await _userService.GetUser(callbackQuery.From.Id, cancellationToken);
        
        await _userService.MuteUser(data.UserId, DateTime.UtcNow.AddDays(1), cancellationToken);
        long count = await _userService.DeleteUserFeedbacks(data.UserId, cancellationToken);
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId,
            MessageTemplate.UserMuted(data.UserId, count), cancellationToken: cancellationToken);
        await _messagesService.SendMenu(botClient, adminUser, cancellationToken);
    }
}