using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Common.Enums;
using Zakup.Entities;

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
    
     public static async ValueTask<ChatInviteLink?> ReplaceInviteLink(this ITelegramBotClient botClient, TelegramAdPost adPost, string additionalText, long userId, bool? needApprove = null)
    {
            
        var chat = await botClient.GetChatAsync(adPost.ChannelId);
        bool isPublic = chat.Type == ChatType.Channel && !string.IsNullOrEmpty(chat.Username);

        try
        {
            // Используем переданное значение needApprove если оно указано, иначе определяем по типу канала
            bool createsJoinRequest = needApprove ?? !isPublic;
            
            // Попытка создать пригласительную ссылку
            var issuedInviteLink = await botClient.CreateChatInviteLinkAsync(
                chatId: adPost.ChannelId,
                name: $"C[{adPost.Title}]{additionalText}",
                createsJoinRequest: createsJoinRequest
            );

            // Обновление URL в сущностях объявления
            foreach (var entity in adPost.Entities)
            {
                if (entity.Type == MessageEntityType.TextLink)
                    entity.Url = issuedInviteLink.InviteLink;
                if (entity.Type == MessageEntityType.Mention)
                {
                    entity.Type = MessageEntityType.TextLink;
                    entity.Url = issuedInviteLink.InviteLink;
                }
            }

            return issuedInviteLink;
        }
        catch (ApiRequestException ex)
        {
            // Специфическая обработка ошибок для недостаточных прав
            if (ex.Message.Contains("not enough rights"))
            {
                await botClient.SendTextMessageAsync(
                    userId,
                    "⚠️ Бот не имеет достаточных прав для управления пригласительными ссылками в вашем канале. " +
                    "Пожалуйста, добавьте бота как администратора с правами на приглашение пользователей и попробуйте снова."
                );
            }
            else
            {
                // Логирование других ошибок
                Console.WriteLine($"Ошибка при создании пригласительной ссылки: {ex.Message}");
                await botClient.SendTextMessageAsync(
                    userId,
                    $"Произошла ошибка при создании пригласительной ссылки: {ex.ErrorCode}"
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            // Общая обработка ошибок для других исключений
            Console.WriteLine($"Непредвиденная ошибка при создании пригласительной ссылки: {ex.Message}");
            await botClient.SendTextMessageAsync(
                userId,
                $"⚠️ Произошла ошибка при создании пригласительной ссылки. Пожалуйста, проверьте права бота в канале и попробуйте снова."
            );
            return null;
        }
    } 
     
    public static async ValueTask<ChatInviteLink> CreateChatInviteLink(this ITelegramBotClient botClient, long chatId, string name, bool createsJoinRequest)
    {
        try
        {
            return await botClient.CreateChatInviteLinkAsync(
                chatId: chatId,
                name: name,
                createsJoinRequest: createsJoinRequest
            );
        }
        catch (ApiRequestException ex)
        {
            Console.WriteLine($"Ошибка при создании пригласительной ссылки: {ex.Message}");
            throw;
        }
    }
    
    
    public static async ValueTask<Message> SendAdPostAsync(this ITelegramBotClient botClient, TelegramAdPost adPost,
        long targetChatId, DocumentsStorageService storageService, CancellationToken cancellationToken = default)
    {
        // todo, guard to unknown entities in message
        adPost.Entities = adPost.Entities.Where(x => x.Type != 0).ToList();
        //
        if (!adPost.MediaGroup?.Documents.Any() ?? true)
            return await botClient.SendTextMessageAsync(targetChatId, adPost.Text, parseMode: null,
                entities: adPost.Entities,
                disableWebPagePreview: true, cancellationToken: cancellationToken /*, replyMarkup: new InlineKeyboardMarkup(buttons)*/);

        if (adPost.MediaGroup.Documents.Count() == 1)
        {
            return await SendWithSingleFile(adPost, adPost.MediaGroup.Documents.First(), botClient,targetChatId, cancellationToken);
            
        }
        
        //Отправка нескольких файлов
        return await SendWithManyFiles(adPost, botClient, targetChatId, storageService, cancellationToken);
    }
    
    private static async Task<Message> SendWithManyFiles(TelegramAdPost post, ITelegramBotClient botClient, long userId, DocumentsStorageService storageService, CancellationToken cancellationToken = default)
    {
        var media = new List<IAlbumInputMedia>();
        var documents = post.MediaGroup.Documents.ToList();
        for (int i = 0; i < documents.Count; i++)
        {
            var document = documents[i];
            var fileStream = await storageService.GetDocument(document.Id);
            string caption = null;
            MessageEntity[]? entities = null;
            ParseMode? parseMode = null;
            InlineKeyboardMarkup replyMarkup = null;
            if (i == 0)
            {
                caption = post.Text;
                entities = post.Entities.ToArray();
                parseMode = ParseMode.MarkdownV2;
                replyMarkup = new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("Замена ссылок..", "replace"));
            }
            IAlbumInputMedia input = document.Kind switch
            {
                TelegramDocumentKind.DOCUMENT => new InputMediaDocument(InputFile.FromStream(fileStream, document.Id.ToString()))
                {
                    Caption = caption, 
                    CaptionEntities = entities,
                    ParseMode = parseMode,
                },
                TelegramDocumentKind.GIF => new InputMediaDocument(InputFile.FromStream(fileStream, document.Id.ToString())) {
                    Caption = caption, 
                    CaptionEntities = entities,
                    ParseMode = parseMode
                },
                TelegramDocumentKind.IMAGE => new InputMediaPhoto(InputFile.FromStream(fileStream, document.Id.ToString())) {
                    Caption = caption, 
                    CaptionEntities = entities,
                    ParseMode = parseMode
                },
                TelegramDocumentKind.VIDEO => new InputMediaVideo(InputFile.FromStream(fileStream, document.Id.ToString())) {
                    Caption = caption, 
                    CaptionEntities = entities,
                    ParseMode = parseMode
                },
                _ => throw new Exception("Unknown document type")
            };
            media.Add(input);
        }
        return (await botClient.SendMediaGroupAsync(
            chatId: userId,
            media: media, cancellationToken: cancellationToken))[0];
    }
    
    private static async Task<Message> SendWithSingleFile(TelegramAdPost adPost, TelegramDocument document, ITelegramBotClient botClient, long targetChatId, CancellationToken cancellationToken)
    {
        return await (document.Kind switch
        {
            TelegramDocumentKind.IMAGE =>  botClient.SendPhotoAsync(targetChatId,
                InputFile.FromString(document.FileId),
                caption: adPost.Text,
                captionEntities: adPost.Entities, cancellationToken: cancellationToken),
            TelegramDocumentKind.GIF => botClient.SendAnimationAsync(targetChatId,
                InputFile.FromString(document.FileId),
                caption: adPost.Text,
                captionEntities: adPost.Entities, cancellationToken: cancellationToken),
            TelegramDocumentKind.VIDEO => botClient.SendVideoAsync(targetChatId,
                InputFile.FromString(document.FileId),
                caption: adPost.Text,
                captionEntities: adPost.Entities, cancellationToken: cancellationToken),
            TelegramDocumentKind.DOCUMENT => botClient.SendDocumentAsync(targetChatId,
                InputFile.FromString(document.FileId),
                caption: adPost.Text,
                captionEntities: adPost.Entities, cancellationToken: cancellationToken),
            _ => throw new ArgumentOutOfRangeException()
        });
    }
    
}