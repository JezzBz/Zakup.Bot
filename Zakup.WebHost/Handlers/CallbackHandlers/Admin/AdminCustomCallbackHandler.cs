using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Admin;

[CallbackType(CallbackType.AdminCustom)]
public class AdminCustomCallbackHandler : ICallbackHandler<EmptyCallbackData>
{
    private readonly InternalSheetsService _sheetsService;

    public AdminCustomCallbackHandler(InternalSheetsService sheetsService)
    {
        _sheetsService = sheetsService;
    }

    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        // await sheetsService.UpdateCreativeTitles();
        // await sheetsService.ClearAndUpdateInviteLinks();
        // await sheetsService.FixSheetColumns();
        await _sheetsService.RenameHeaderColumnsAsync();
    }
}