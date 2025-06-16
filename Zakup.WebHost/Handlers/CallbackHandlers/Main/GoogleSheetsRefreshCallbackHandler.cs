using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.Main;

[CallbackType(CallbackType.RefreshGoogleSheets)]
public class GoogleSheetsRefreshCallbackHandler : IEmptyCallbackHandler
{
    private readonly InternalSheetsService _sheetsService;

    public GoogleSheetsRefreshCallbackHandler(InternalSheetsService sheetsService)
    {
        _sheetsService = sheetsService;
    }

    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        await _sheetsService.UpdateStatistic(callbackQuery.From.Id);
        await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.DataRefreshed, cancellationToken: cancellationToken);
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
    }
}