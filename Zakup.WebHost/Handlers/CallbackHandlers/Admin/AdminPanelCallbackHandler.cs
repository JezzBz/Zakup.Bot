using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.Enums;
using Zakup.Abstractions.Data;
using Zakup.Common.DTO.Admin;
using Zakup.WebHost.Helpers;
using Zakup.WebHost.Constants;

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
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Statistic, ((int)CallbackType.AdminStatistics).ToString()),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Broadcast, ((int)CallbackType.AdminBroadcast).ToString()),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Mute, ((int)CallbackType.AdminMute).ToString()),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Scam, ((int)CallbackType.AdminScam).ToString()),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Unmute, ((int)CallbackType.AdminUnmute).ToString()),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Unscam, ((int)CallbackType.AdminUnscam).ToString()),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Custom, ((int)CallbackType.AdminCustom).ToString()),
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