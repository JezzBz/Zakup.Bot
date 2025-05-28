using Bot.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Services;

namespace Zakup.WebHost.Handlers;

public class MediaGroupUploadHandler :   IUpdatesHandler
{
    private readonly DocumentsStorageService _documentsStorageService;

    public MediaGroupUploadHandler(DocumentsStorageService documentsStorageService)
    {
        _documentsStorageService = documentsStorageService;
    }

    public static bool ShouldHandle(Update update)
    {
        return update.Message?.Photo != null || update.Message?.Video != null || update.Message?.Document != null || update.Message?.Animation != null;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        await _documentsStorageService.SaveDocuments(botClient, update.Message, update.Message.MediaGroupId);
    }
}