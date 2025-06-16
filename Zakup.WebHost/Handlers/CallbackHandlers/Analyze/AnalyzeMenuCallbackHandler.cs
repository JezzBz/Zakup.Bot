using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Analyze;

[CallbackType(CallbackType.AnalyzeMenu)]
public class AnalyzeMenuCallbackHandler : IEmptyCallbackHandler
{
    private readonly AnalyzeService _analyzeService;

    public AnalyzeMenuCallbackHandler(AnalyzeService analyzeService)
    {
        _analyzeService = analyzeService;
    }

    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var balance = await _analyzeService.GetAnalyzePoints(callbackQuery.From.Id, cancellationToken);
        var messageText = MessageTemplate.AnalyzeMenuText(balance);

        var keyboard = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.BuyPoints,
                ((int)CallbackType.AnalyzeBuyPoints).ToString())
        };
        
        if (balance > 0)
        {
            keyboard.Add(
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.AnalyzeStart, ((int)CallbackType.AnalyzeInit).ToString())
                );
        }
        
        var markUp = new InlineKeyboardMarkup(keyboard);
    }
}