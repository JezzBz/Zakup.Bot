using Bot.Core;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.EntityFramework;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.SystemHandlers;

//TODO:fix files
public class InlineQueryHandler : IUpdatesHandler
{
    private readonly ApplicationDbContext _dataContext;
    private readonly MetadataStorage _metadataStorage;
    private readonly HandlersManager _handlersManager;

    public InlineQueryHandler(ApplicationDbContext dataContext, MetadataStorage metadataStorage, HandlersManager handlersManager)
    {
        _dataContext = dataContext;
        _metadataStorage = metadataStorage;
        _handlersManager = handlersManager;
    }

    public static bool ShouldHandle(Update update)
    {
        return update.Type == UpdateType.InlineQuery;
    }

     public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var inlineQuery = update.InlineQuery!;
         if (string.IsNullOrEmpty(inlineQuery.Query))
        {
            return;
        }

        _metadataStorage.PostMetadataStorage.TryRemove(inlineQuery.From.Id, out _);
        _metadataStorage.PostMetadataStorage.TryAdd(inlineQuery.From.Id, inlineQuery.Query);
        

        var text = inlineQuery.Query;
        var textParts = inlineQuery.Query.Trim().Split(" ");

        if (textParts.Length < 3)
            {
                await botClient.AnswerInlineQueryAsync(inlineQuery.Id, new List<InlineQueryResult>(), cancellationToken: cancellationToken);
                return;
            }

        var alias = textParts[^1].ToLowerInvariant();; // Последний элемент - алиас
        var priceText = textParts[^2].ToLowerInvariant(); // Предпоследний элемент - цена

        string kreoTitle = null; decimal price; // Инициализируем переменную
        List<TelegramAdPost> posts = new List<TelegramAdPost>(); 

        if (!decimal.TryParse(textParts[^2], out price))
        {
            // Если второй элемент с конца не является числом, проверяем третий элемент с конца
            if (!decimal.TryParse(textParts[^3], out price))
            {
                // Если и третий элемент с конца не является числом, выбрасываем исключение
                throw new InvalidOperationException("Некорректный формат цены.");
            }
            posts = await _dataContext.TelegramAdPosts
                .Where(x => x.Title == alias && x.Channel.Alias == priceText)
                .Where(x => x.Channel.Administrators.Any(z => z.Id == inlineQuery.From.Id))
                .Where(x => !x.HasDeleted)
                .Include(q => q.MediaGroup)
                .ThenInclude(q => q.Documents)
                .ToListAsync(cancellationToken: cancellationToken);
        }
        else
        {
            posts = await _dataContext.TelegramAdPosts
                .Where(x => x.Channel.Alias != null)
                // .Where(x => text.Contains(x.Channel.Alias!))
                .Where(x => x.Channel.Administrators.Any(z => z.Id == inlineQuery.From.Id))
                .Where(x => x.Channel.Alias != null && x.Channel.Alias == alias)
                .Where(x => !x.HasDeleted)
                .Where(x => !string.IsNullOrWhiteSpace(x.Title)).Include(telegramAdPost => telegramAdPost.MediaGroup)
                .ThenInclude(mediaGroup => mediaGroup.Documents)
                .ToListAsync(cancellationToken: cancellationToken);
        }

        if (posts.Count == 0)
        {
            await botClient.AnswerInlineQueryAsync(inlineQuery.Id, new List<InlineQueryResult>(), cancellationToken: cancellationToken);
            Console.WriteLine($"Постов нет. Запрос: '{text}'");
            return;
        }

        var results = new List<InlineQueryResult>();
        
        
       
        foreach (var site in posts)
        {
            // Console.WriteLine($"показываю крео {site.Title}");
            // foreach (var entity in site.Entities)
            // {
            //     Console.WriteLine($"MessageEntity Type: {entity.Type}, Offset: {entity.Offset}, Length: {entity.Length}");
            // }
           
            if (site.MediaGroup?.Documents.Any() ?? false)
            {
                var file = site.MediaGroup.Documents.First();
                var kind = file.Kind;
               
                results.Add(kind switch
                {
                    TelegramDocumentKind.IMAGE => new InlineQueryResultCachedPhoto($"{site.Id}", file.FileId)
                    {
                        Title = site.Title,
                        Caption = site.Text,
                        CaptionEntities = site.Entities.ToArray(),
                        ReplyMarkup = 
                            new InlineKeyboardMarkup(
                                InlineKeyboardButton.WithCallbackData("Замена ссылок..", "replace"))
                       
                    },
                    TelegramDocumentKind.GIF => new InlineQueryResultCachedGif($"{site.Id}", file.FileId)
                    {
                        Title = site.Title,
                        Caption = site.Text,
                        CaptionEntities = site.Entities.ToArray(),
                        ReplyMarkup =
                            new InlineKeyboardMarkup(
                                InlineKeyboardButton.WithCallbackData("Замена ссылок..", "replace")) 
                    },
                    TelegramDocumentKind.VIDEO => new InlineQueryResultCachedPhoto($"{site.Id}", file.ThumbnailId)
                    {
                        Title = site.Title,
                        Caption = site.Text,
                        CaptionEntities = site.Entities.ToArray(),
                        ReplyMarkup =
                            new InlineKeyboardMarkup(
                                InlineKeyboardButton.WithCallbackData("Замена ссылок..", "replace"))
                    },
                    TelegramDocumentKind.DOCUMENT => new InlineQueryResultCachedDocument($"{site.Id}", file.FileId, site.Title)
                    {
                        Caption = site.Text,
                        CaptionEntities = site.Entities.ToArray(),
                        ReplyMarkup =
                            new InlineKeyboardMarkup(
                                InlineKeyboardButton.WithCallbackData("Замена ссылок..", "replace")) 
                    },
                });
            }
            else
            {
                var i = new InlineQueryResultArticle($"{site.Id}", site.Title,
                    new InputTextMessageContent(site.Text)
                    {
                        Entities = site.Entities.ToArray(),
                        DisableWebPagePreview = true
                    })
                {
                    ReplyMarkup =
                        new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Замена ссылок..", "replace"))
                };
                results.Add(i);
            }
        }
    // }
   
    await botClient.AnswerInlineQueryAsync(inlineQuery.Id, results, cacheTime: 0, isPersonal: true, cancellationToken: cancellationToken);
    }

}