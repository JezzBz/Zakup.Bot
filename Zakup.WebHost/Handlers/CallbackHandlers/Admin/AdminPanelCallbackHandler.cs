using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.Enums;
using Zakup.Abstractions.Data;
using Zakup.Common.DTO.Admin;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Admin;

[CallbackType(CallbackType.AdminPanel)]
public class AdminPanelCallbackHandler : ICallbackHandler<AdminPanelCallbackData>
{
    public async Task Handle(ITelegramBotClient botClient, AdminPanelCallbackData data, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Мут на оценки", ((int)CallbackType.AdminMute).ToString()),
                InlineKeyboardButton.WithCallbackData("Статистика", ((int)CallbackType.AdminStatistics).ToString()),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Скам-метка", ((int)CallbackType.AdminScam).ToString()),
                InlineKeyboardButton.WithCallbackData("Удалить канал", ((int)CallbackType.AdminDelete).ToString()),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Размут", ((int)CallbackType.AdminUnmute).ToString()),
                InlineKeyboardButton.WithCallbackData("Снять скам", ((int)CallbackType.AdminUnscam).ToString()),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Рассылка", ((int)CallbackType.AdminBroadcast).ToString()),
                InlineKeyboardButton.WithCallbackData("Custom", ((int)CallbackType.AdminCustom).ToString()),
            }
        });

        await botClient.EditMessageTextAsync(
            callbackQuery.Message.Chat.Id,
            callbackQuery.Message.MessageId,
            "Добро пожаловать в панель администратора!",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }
} 