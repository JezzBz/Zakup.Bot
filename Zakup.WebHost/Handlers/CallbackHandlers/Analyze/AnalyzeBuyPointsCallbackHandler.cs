using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Analyze;

[CallbackType(CallbackType.AnalyzeBuyPoints)]
public class AnalyzeBuyPointsCallbackHandler : IEmptyCallbackHandler
{
    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId, MessageTemplate.AnalyzePointsBuyText, cancellationToken: cancellationToken);
    }
}