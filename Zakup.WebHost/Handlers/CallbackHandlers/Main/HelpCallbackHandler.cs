using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.Main;

[CallbackType(CallbackType.Help)]
public class HelpCallbackHandler : ICallbackHandler<EmptyCallbackData>
{
    private readonly HandlersManager _handlersManager;

    public HelpCallbackHandler(HandlersManager handlersManager)
    {
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var index = 0;
        var buttons = new List<List<InlineKeyboardButton>>();
        foreach (var question in MessageTemplate.Help.Questions)
        {
            var callbackData = await _handlersManager.ToCallback(new HelpQuestionCallbackData
            {
                QuestionId = index
            });
            
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(question,callbackData)
            });
            
            index++;
        }
        
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(HelpMessageTemplate.HelpAnOffer, nameof(CallbackType.HelpAnOffer))
        });
        
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, $"menu|{callbackQuery.Message.MessageId}")
        });

        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId,
            HelpMessageTemplate.ChooseQuestion, replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
    }

    public EmptyCallbackData Parse(List<string> parameters)
    {
        return new EmptyCallbackData();
    }
}