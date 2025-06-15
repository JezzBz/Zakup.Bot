using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.StateMessageHandlers.Admin;

[StateType(UserStateType.AdminUnmute)]
public class UnMuteAdminHandler : IStateHandler
{
    private readonly UserService _userService;
    private readonly MessagesService _messagesService;

    public UnMuteAdminHandler(UserService userService, MessagesService messagesService)
    {
        _userService = userService;
        _messagesService = messagesService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        
        if (string.IsNullOrEmpty(message.Text))
            throw new InvalidOperationException();
        if (!long.TryParse(message.Text, out var userId))
        {
            if (message.ForwardFrom is null)
                throw new InvalidOperationException();
            userId = message.ForwardFrom.Id;
        }
        await _userService.UnMute(userId, cancellationToken);
        var adminUser = await _userService.GetUser(message.From.Id,cancellationToken);
        await botClient.SafeEdit(adminUser.Id, adminUser.UserState.PreviousMessageId, MessageTemplate.UserUnMuted, cancellationToken: cancellationToken);
        await _messagesService.SendMenu(botClient, adminUser, cancellationToken);
    }
}