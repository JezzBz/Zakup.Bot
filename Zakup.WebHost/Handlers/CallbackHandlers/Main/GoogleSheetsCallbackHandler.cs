using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Main;

[CallbackType(CallbackType.GoogleSheets)]
public class GoogleSheetsCallbackHandler : IEmptyCallbackHandler
{
    private readonly InternalSheetsService _sheetsService;

    public GoogleSheetsCallbackHandler(InternalSheetsService sheetsService)
    {
        _sheetsService = sheetsService;
    }

    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
       var sheet = await _sheetsService.GetUserSheet(callbackQuery.From.Id);
       
        if (sheet == null)
        {
            await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.SheetNotExist, cancellationToken: cancellationToken);
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }
        
        var buttons = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.RefreshData, ((int)CallbackType.RefreshGoogleSheets).ToString()),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, $"menu|{callbackQuery.Message!.MessageId}")
        };
       
        await botClient.SafeEdit(
            callbackQuery.From.Id, 
            callbackQuery.Message.MessageId, 
            MessageTemplate.GoogleSheetsText(sheet.Id),
            parseMode: ParseMode.MarkdownV2, 
            replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
        
    }
}