using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Zakup.WebHost.Extensions;

public static class DefaultHandlerConditionsExtensions
{
    public static bool IsMessage(this Update update) => update.Type == UpdateType.Message;
    
    public static bool IsCallback(this Update update) => update.Type == UpdateType.CallbackQuery;
    
    public static bool IsNotEmptyMessage(this Update update) =>
        update.IsMessage() && !string.IsNullOrEmpty(update.Message?.Text);
    
    
}