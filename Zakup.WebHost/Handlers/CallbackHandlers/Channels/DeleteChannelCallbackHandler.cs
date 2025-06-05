using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.DeleteChannel)]
public class DeleteChannelCallbackHandler : ICallbackHandler<DeleteChannelCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly HandlersManager _handlersManager;
    public DeleteChannelCallbackHandler(ChannelService channelService, HandlersManager handlersManager)
    {
        _channelService = channelService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, DeleteChannelCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var yesData = await _handlersManager.ToCallback(new DeleteChannelCompleteCallbackData()
        {
            ChannelId = data.ChannelId
        });
        var noData = await _handlersManager.ToCallback(new ShowChannelMenuCallbackData()
        {
            ChannelId = data.ChannelId
        });
        
        
        var keyboard = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Yes, yesData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.No, noData)
        };
        await botClient.SafeEdit(callbackQuery.From.Id,
            callbackQuery.Message!.MessageId, 
            MessageTemplate.DeleteChannelAlert,
            replyMarkup: new InlineKeyboardMarkup(keyboard),
            cancellationToken: cancellationToken);
    }
}