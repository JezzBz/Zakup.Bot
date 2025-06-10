using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.ZakupPayed)]
public class ZakupPayedCallbackHandler : ICallbackHandler<ZakupPayedCallbackData>
{
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;

    public ZakupPayedCallbackHandler(ZakupService zakupService, HandlersManager handlersManager)
    {
        _zakupService = zakupService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, ZakupPayedCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var zakup = await _zakupService.Get(data.ZakupId, includeAll:true, cancellationToken: cancellationToken);
        zakup.IsPad = true;
        await _zakupService.Update(zakup, cancellationToken);
        var deleteData = await _handlersManager.ToCallback(new  DeleteZakupRequestCallbackData
        {
            ZakupId = data.ZakupId
        });
        var markUp = new List<InlineKeyboardButton>()
        { 
            InlineKeyboardButton.WithCallbackData("⚙️Изменить", $"zakup:post:{ZakupPostFlowType.UPDATE}:{zakup.Id}"),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Delete, deleteData)
        };
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId,
            (callbackQuery.Message.Text ?? callbackQuery.Message.Caption).Replace(MessageTemplate.ZakupNotPayed, MessageTemplate.ZakupPayed), replyMarkup: new InlineKeyboardMarkup(markUp), cancellationToken: cancellationToken);
    }
}