using Bot.Core;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.EntityFramework;
using Zakup.Services;

namespace Zakup.WebHost.Handlers.MessageHandlers;

public class TestHandler : IUpdatesHandler
{
    private readonly ApplicationDbContext _context;
    private readonly DocumentsStorageService _storageService;

    public TestHandler(ApplicationDbContext context, DocumentsStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    public static bool ShouldHandle(Update update)
    {
        return false;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var documents = await _context.TelegramDocuments.AsQueryable()
            .Take(2)
            .ToListAsync(cancellationToken: cancellationToken);

        var fileStream = await _storageService.GetDocument(documents[0].Id);
        var fileStream2 = await _storageService.GetDocument(documents[1].Id);
        var markUp = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("Замена ссылок..", "replace"));
        var media = new List<IAlbumInputMedia>();
        var first = new InputMediaPhoto(InputFile.FromStream(fileStream, documents[0].Id.ToString()))
        {
            Caption = "AAA",
            ParseMode = ParseMode.MarkdownV2
        };
        var second = new InputMediaPhoto(InputFile.FromStream(fileStream2, documents[1].Id.ToString()))
        {
        };
        media.Add(first);
        media.Add(second);
        var message2 = await botClient.SendTextMessageAsync(update.Message.From.Id, "Замена ссылок", replyMarkup:markUp); 
        var message = await botClient.SendMediaGroupAsync(update.Message.From.Id, media, replyToMessageId:message2.MessageId);
        
    }
}