using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Zakup.Services.Extensions;

public static class BotClientExtensions
{
    public static async Task SafeDelete(this ITelegramBotClient botClient, long chatId, int messageId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(botClient);
        
        try 
        {
            await botClient.DeleteMessageAsync(chatId, messageId, cancellationToken: cancellationToken);
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

    public static async Task<Message> SafeEdit(this ITelegramBotClient botClient, long chatId, int messageId, string text,  ParseMode? parseMode = null, 
        IEnumerable<MessageEntity>? entities = null, 
        bool? disableWebPagePreview = null, 
        InlineKeyboardMarkup? replyMarkup = null, 
        CancellationToken cancellationToken = default(CancellationToken))
    {
        try
        {
           return await botClient.EditMessageTextAsync(chatId,
                messageId,
                text,
                parseMode,
                replyMarkup: replyMarkup,
                disableWebPagePreview: disableWebPagePreview,
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex) 
        { 
            Console.WriteLine($"[SafeEdit] Сообщение не может быть отредактировано: {chatId}/{messageId}");
            return await botClient.SendTextMessageAsync(chatId,
                text,
                null, 
                parseMode, 
                entities,
                disableWebPagePreview, 
                null,
                null,
                null, 
                null,
                replyMarkup,  
                cancellationToken: 
                cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SafeEdit] Critical error: {ex}");
        }

        return null;
    }
}