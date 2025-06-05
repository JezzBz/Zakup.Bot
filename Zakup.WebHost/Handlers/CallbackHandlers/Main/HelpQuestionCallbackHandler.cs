using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.Main;

[CallbackType(CallbackType.HelpQuestion)]
public class HelpQuestionCallbackHandler : ICallbackHandler<HelpQuestionCallbackData>
{
    public async Task Handle(ITelegramBotClient botClient, HelpQuestionCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var response = MessageTemplate.Help.Responses[data.QuestionId];
        
        var keyboard = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, nameof(CallbackType.Help))
            }
        });
        
        await botClient.SafeEdit(
            callbackQuery.From.Id,
            callbackQuery.Message!.MessageId,
            response,
            parseMode: ParseMode.Markdown,
            replyMarkup: keyboard, cancellationToken: cancellationToken);
    }
}