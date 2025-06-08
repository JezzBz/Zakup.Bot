using Bot.Core;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
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
        return false; //TODO: баг с алиасом в закупе с премиумом
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {

        //var message = await botClient.Send(-1002792996108, update.Message.Text, entities: update.Message.Entities );
        // await botClient.ForwardMessageAsync(512083234, -1002792996108,message.MessageId
        //     ,cancellationToken: cancellationToken);
    }
}