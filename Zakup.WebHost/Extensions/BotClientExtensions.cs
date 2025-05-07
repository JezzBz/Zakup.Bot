using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace Zakup.WebHost.Extensions;

public static class BotClientExtensions
{
    public static async Task SafeDelete(this ITelegramBotClient? botClient, long chatId, int messageId)
    {
        ArgumentNullException.ThrowIfNull(botClient);
        
        try 
        {
            await botClient.DeleteMessageAsync(chatId, messageId);
        }
        catch (ApiRequestException ex) when (ex.Message.Contains("message to delete not found"))
        { 
            Console.WriteLine($"[SafeDelete] Сообщение уже удалено: {chatId}/{messageId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SafeDelete] Critical error: {ex}");
        }
    }
}