using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.Main;

[CallbackType(CallbackType.HelpAnOffer)]
public class HelpAnOfferCallbackHandler : IEmptyCallbackHandler
{
    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            using var stream = System.IO.File.OpenRead("oferta.docx");
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
            await botClient.SendDocumentAsync(
                callbackQuery.From.Id,
                new InputFileStream(stream, "Публичная оферта.docx"),
                caption: HelpMessageTemplate.HelpAnOffer, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при отправке файла оферты: {ex}");
            await botClient.SendTextMessageAsync(
                callbackQuery.From.Id,
                MessageTemplate.CantSendFileError,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, $"{CallbackType.Help}") }
                }), cancellationToken: cancellationToken);
        }
    }
}