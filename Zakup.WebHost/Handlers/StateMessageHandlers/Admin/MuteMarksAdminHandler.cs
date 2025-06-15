using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Admin;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.StateMessageHandlers.Admin;

[StateType(UserStateType.AdminMute)]
public class MuteMarksAdminHandler : IStateHandler
{
    private readonly HandlersManager _handlersManager;
    private readonly UserService _userService;
    
    public MuteMarksAdminHandler(HandlersManager handlersManager, UserService userService)
    {
        _handlersManager = handlersManager;
        _userService = userService;
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
        
        var state = await _userService.GetUserState(message.From.Id, cancellationToken);
        
        var callbackData = await _handlersManager.ToCallback(new AdminConfirmMuteCallbackData
        {
            UserId = userId
        });
        
        var keyboard = new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Yes, callbackData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.No, $"menu|{state.PreviousMessageId}"),
        };
        
        await botClient.SafeEdit(message.From.Id ,state.PreviousMessageId, MessageTemplate.MuteRequestConfirm(message.ForwardFrom?.Username), replyMarkup:new InlineKeyboardMarkup(keyboard), cancellationToken: cancellationToken);
    }
}