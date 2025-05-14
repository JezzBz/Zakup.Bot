using Telegram.Bot.Types.ReplyMarkups;

namespace Zakup.WebHost.Helpers.Callback;

public static class InternalKeyboard
{
    public static InlineKeyboardMarkup FromButton(InlineKeyboardButton button) => new InlineKeyboardMarkup([[button]]);
}