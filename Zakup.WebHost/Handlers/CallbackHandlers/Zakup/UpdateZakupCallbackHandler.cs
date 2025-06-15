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
using Zakup.WebHost.Services;
using System.Text;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.UpdateZakup)]
public class UpdateZakupCallbackHandler : ICallbackHandler<UpdateZakupCallbackData>
{
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;
    private readonly ZakupMessageService _zakupMessageService;

    public UpdateZakupCallbackHandler(ZakupService zakupService, HandlersManager handlersManager, ZakupMessageService zakupMessageService)
    {
        _zakupService = zakupService;
        _handlersManager = handlersManager;
        _zakupMessageService = zakupMessageService;
    }

    public async Task Handle(ITelegramBotClient botClient, UpdateZakupCallbackData data, CallbackQuery callbackQuery, CancellationToken cancellationToken)
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

        var keyboard = await _zakupMessageService.GetEditMenuKeyboard(data.ZakupId, cancellationToken);

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
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
} 