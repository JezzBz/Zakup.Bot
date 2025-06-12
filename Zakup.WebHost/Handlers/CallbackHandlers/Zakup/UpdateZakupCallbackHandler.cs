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
using System.Text;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.UpdateZakup)]
public class UpdateZakupCallbackHandler : ICallbackHandler<UpdateZakupCallbackData>
{
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;

    public UpdateZakupCallbackHandler(ZakupService zakupService, HandlersManager handlersManager)
    {
        _zakupService = zakupService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, UpdateZakupCallbackData data, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var zakup = await _zakupService.Get(data.ZakupId, includeAll: true, cancellationToken);
        if (zakup == null)
            throw new ArgumentException($"Zakup with id {data.ZakupId} not found");

        var changePriceData = await _handlersManager.ToCallback(new ChangePriceCallbackData
        {
            ZakupId = data.ZakupId
        });
        
        var changeDateData = await _handlersManager.ToCallback(new ChangeZakupDateCallbackData
        {
            ZakupId = data.ZakupId
        });

        var updateData = await _handlersManager.ToCallback(new UpdateZakupCallbackData
        {
            ZakupId = data.ZakupId
        });

        var markUp = new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.ChangePrice, changePriceData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.ChangeDate, changeDateData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, updateData)
        };

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