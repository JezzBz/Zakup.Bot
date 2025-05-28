using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.NewChannel)]
public class AddChannelCallbackHandler : ICallbackHandler<EmptyCallbackData>
{
    private readonly UserService _userService;

    public AddChannelCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        
        state!.State = UserStateType.ConfirmAddChannel;
        var inlineKeyboard = GetKeyboardMarkup(callbackQuery.Message!.MessageId);
        
        await botClient.SafeEdit(
            chatId: callbackQuery.From.Id,
            messageId: callbackQuery.Message!.MessageId,
            text: MessageTemplate.AddChannelRequest,
            parseMode: ParseMode.Markdown,
            replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        
        await _userService.SetUserState(state.UserId, state, cancellationToken);
    }

    public EmptyCallbackData Parse(List<string> parameters)
    {
       return new EmptyCallbackData();
    }

    private InlineKeyboardMarkup GetKeyboardMarkup(long msgId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl(ButtonsTextTemplate.AddBot, "https://t.me/zakup_robot?startchannel&admin=invite_users"),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Menu, $"menu|{msgId}")
            }
        });
    }
}