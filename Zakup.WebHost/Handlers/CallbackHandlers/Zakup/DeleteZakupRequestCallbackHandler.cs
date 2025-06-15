using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.DeleteZakupRequest)]
public class DeleteZakupRequestCallbackHandler : ICallbackHandler<DeleteZakupRequestCallbackData>
{
    private readonly HandlersManager _handlersManager;

    public DeleteZakupRequestCallbackHandler(HandlersManager handlersManager)
    {
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, DeleteZakupRequestCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
       var deleteData = await _handlersManager.ToCallback(new DeleteZakupCallbackData()
       {
           ZakupId = data.ZakupId
       });

       var backData = await _handlersManager.ToCallback(new ReturnToMainMenuCallbackData
       {
           ZakupId = data.ZakupId
       });
       
       var keyboard = new List<InlineKeyboardButton>()
       {
           InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Delete, deleteData),
           InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, backData),
       };

       await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId, MessageTemplate.DeleteZakupAlert,
           replyMarkup: new InlineKeyboardMarkup(keyboard), cancellationToken: cancellationToken);
    }
}