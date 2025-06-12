using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Services;

public class ZakupMessageService
{
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;

    public ZakupMessageService(ZakupService zakupService, HandlersManager handlersManager)
    {
        _zakupService = zakupService;
        _handlersManager = handlersManager;
    }

    public async Task<(string Message, InlineKeyboardMarkup Keyboard)> GetZakupMessageAndKeyboard(Guid zakupId,
        CancellationToken cancellationToken)
    {
        var zakup = await _zakupService.Get(zakupId, includeAll: true, cancellationToken: cancellationToken);
        if (zakup == null)
        {
            throw new ArgumentException("Закуп не найдена");
        }

        var deleteData = await _handlersManager.ToCallback(new DeleteZakupRequestCallbackData
        {
            ZakupId = zakupId
        });

        var updateData = await _handlersManager.ToCallback(new UpdateZakupCallbackData
        {
            ZakupId = zakupId
        });

        var markUp = new List<InlineKeyboardButton>();
        
        if (!zakup.IsPad)
        {
            var payData = await _handlersManager.ToCallback(new ZakupPayedCallbackData
            {
                ZakupId = zakupId
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

        return (messageBuilder.ToString(), new InlineKeyboardMarkup(markUp));
    }

    public async Task<InlineKeyboardMarkup> GetEditMenuKeyboard(Guid zakupId, CancellationToken cancellationToken)
    {
        var changePriceData = await _handlersManager.ToCallback(new ChangePriceCallbackData
        {
            ZakupId = zakupId
        });

        var changeDateData = await _handlersManager.ToCallback(new ChangeZakupDateCallbackData()
        {
            ZakupId = zakupId
        });

        var returnData = await _handlersManager.ToCallback(new ReturnToMainMenuCallbackData
        {
            ZakupId = zakupId
        });

        var markUp = new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.ChangePrice, changePriceData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.ChangeDate, changeDateData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, returnData)
        };

        return new InlineKeyboardMarkup(markUp);
    }
} 