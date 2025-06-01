using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Post;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.AdPosts;

[CallbackType(CallbackType.GenerateAdPost)]
public class GenerateAdPostCallbackHandler : ICallbackHandler<GenerateAdPostCallbackData>
{
    private readonly AdPostsService _adPostsService;
    private readonly DocumentsStorageService _documentsStorageService;
    
    public GenerateAdPostCallbackHandler(AdPostsService adPostsService, DocumentsStorageService documentsStorageService)
    {
        _adPostsService = adPostsService;
        _documentsStorageService = documentsStorageService;
    }

    public async Task Handle(ITelegramBotClient botClient, GenerateAdPostCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var post = await _adPostsService.Get(data.PostId, cancellationToken, true);
        post!.Entities = post.Entities.Where(x => x.Type != 0).ToList();
        
        if (!post.MediaGroup.Documents.Any())
        {
            await botClient.SendTextMessageAsync(callbackQuery.From.Id, post.Text, parseMode: null,
                entities: post.Entities,
                disableWebPagePreview: true, cancellationToken: cancellationToken);
            return;
        }

        if (post.MediaGroup.Documents.Count() == 1)
        {
            await SendWithSingleFile(post, post.MediaGroup.Documents.First(), botClient, callbackQuery.From.Id, cancellationToken);
            return;
        }
        
        //Отправка нескольких файлов
        await SendWithManyFiles(post, botClient, callbackQuery, cancellationToken);
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
    }

    private async Task SendWithManyFiles(TelegramAdPost post, ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var media = new List<IAlbumInputMedia>();
        var documents = post.MediaGroup.Documents.ToList();
        for (int i = 0; i < documents.Count; i++)
        {
            var document = documents[i];
            var fileStream = await _documentsStorageService.GetDocument(document.Id);
            string caption = null;
            MessageEntity[]? entities = null;
            ParseMode? parseMode = null;
            if (i == 0)
            {
                caption = post.Text;
                entities = post.Entities.ToArray();
                parseMode = ParseMode.MarkdownV2;
            }
            IAlbumInputMedia input = document.Kind switch
            {
                TelegramDocumentKind.DOCUMENT => new InputMediaDocument(InputFile.FromStream(fileStream, document.Id.ToString()))
                {
                    Caption = caption, 
                    CaptionEntities = entities,
                    ParseMode = parseMode
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
        
        await botClient.SendMediaGroupAsync(
            chatId: callbackQuery.From.Id,
            media: media, cancellationToken: cancellationToken);
    }
    
    private async Task SendWithSingleFile(TelegramAdPost adPost, TelegramDocument document, ITelegramBotClient botClient, long targetChatId, CancellationToken cancellationToken)
    {
        await (document.Kind switch
        {
            TelegramDocumentKind.IMAGE => botClient.SendPhotoAsync(targetChatId,
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
    
    public GenerateAdPostCallbackData Parse(List<string> parameters)
    {
        return new GenerateAdPostCallbackData
        {
            PostId = Guid.Parse(parameters[0])
        };
    }
}