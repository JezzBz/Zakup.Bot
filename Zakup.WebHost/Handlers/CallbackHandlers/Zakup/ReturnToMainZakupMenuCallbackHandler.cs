using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.ReturnToZakupMainMenu)]
public class ReturnToMainZakupMenuCallbackHandler : ICallbackHandler<ReturnToMainMenuCallbackData>
{
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;

    public ReturnToMainZakupMenuCallbackHandler(ZakupService zakupService, HandlersManager handlersManager)
    {
        _zakupService = zakupService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, ReturnToMainMenuCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var zakup = await _zakupService.Get(data.ZakupId, includeAll: true, cancellationToken: cancellationToken);
        if (zakup == null)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Закуп не найдена",
                cancellationToken: cancellationToken);
            return;
        }

        var deleteData = await _handlersManager.ToCallback(new DeleteZakupRequestCallbackData
        {
            ZakupId = data.ZakupId
        });

        var updateData = await _handlersManager.ToCallback(new UpdateZakupCallbackData
        {
            ZakupId = data.ZakupId
        });

        var markUp = new List<InlineKeyboardButton>();
        
        if (!zakup.IsPad)
        {
            var payData = await _handlersManager.ToCallback(new ZakupPayedCallbackData
            {
                ZakupId = data.ZakupId
            });
            markUp.Add(InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.MarkAsPaid, payData));
        }

        markUp.Add(InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Edit, updateData));
        markUp.Add(InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Delete, deleteData));

        var messageBuilder = new StringBuilder($"🔥Запланировано размещение для вашего канала [{zakup.Channel.Title}].");
        messageBuilder.AppendLine("");
        messageBuilder.AppendLine($"Тип ссылки: {(zakup.NeedApprove ? "Закрытая" : "Открытая")}");
        messageBuilder.AppendLine($"💸Цена: {zakup.Price}");
        messageBuilder.AppendLine($"📣Платформа: {zakup.Platform}");
        messageBuilder.AppendLine($"📅Дата публикации: {zakup.PostTime?.AddHours(3):dd.MM.yyyy HH:mm}");
        messageBuilder.AppendLine($"Креатив: {zakup.AdPost?.Title ?? "Не выбран"}");
        messageBuilder.AppendLine($"Оплачено: {(zakup.IsPad ? "Да✅" : "Нет❌")}");

        await botClient.EditMessageTextAsync(
            callbackQuery.Message!.Chat.Id,
            callbackQuery.Message.MessageId,
            messageBuilder.ToString(),
            replyMarkup: new InlineKeyboardMarkup(markUp),
            cancellationToken: cancellationToken);
    }
} 