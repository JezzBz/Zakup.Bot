using System.Globalization;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.StateMessageHandlers.Zakup;

[StateType(UserStateType.ChangeZakupDate)]
public class ZakupChangeDateHandler : IStateHandler
{
    private readonly UserService _userService;
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;

    public ZakupChangeDateHandler(UserService userService, ZakupService zakupService, HandlersManager handlersManager)
    {
        _userService = userService;
        _zakupService = zakupService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message == null)
        {
            return;
        }

        if (!DateTime.TryParseExact(update.Message.Text, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            await botClient.SendTextMessageAsync(update.Message.From!.Id, MessageTemplate.ChooseZakupDate,
                cancellationToken: cancellationToken);
            return;
        }
        
        var state = await _userService.GetUserState(update.Message.From!.Id, cancellationToken);
        var zakupId = CacheHelper.ToData<ZakupIdCache>(state.CachedValue)!.ZakupId;
        var zakup = await _zakupService.Get(zakupId, includeAll: true, cancellationToken: cancellationToken);
        
        if (zakup == null)
        {
            await botClient.SendTextMessageAsync(
                update.Message.Chat.Id,
                "Закуп не найдена",
                cancellationToken: cancellationToken);
            return;
        }

        // Преобразуем локальное время в UTC
        zakup.PostTime = date.ToUniversalTime();
        await _zakupService.Update(zakup, cancellationToken);
        
        state.Clear();
        await _userService.SetUserState(state, cancellationToken);

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

        await botClient.SendTextMessageAsync(
            update.Message.Chat.Id,
            messageBuilder.ToString(),
            replyMarkup: new InlineKeyboardMarkup(markUp),
            cancellationToken: cancellationToken);

        await botClient.DeleteMessageAsync(
            update.Message.Chat.Id,
            update.Message.MessageId,
            cancellationToken: cancellationToken);
    }
} 