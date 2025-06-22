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

[CallbackType(CallbackType.AddChannelChat)]
public class AddChannelChatCallbackHandler : ICallbackHandler<AddChannelChatCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly UserService _userService;
    private readonly HandlersManager _handlersManager;
    public AddChannelChatCallbackHandler(ChannelService channelService, UserService userService, HandlersManager handlersManager)
    {
        _channelService = channelService;
        _userService = userService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, AddChannelChatCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var channel = await _channelService.GetChannel(data.ChannelId, cancellationToken);
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

        if (channel?.ChannelChatId is not null)
        {
            return;
        }

        var skipData = await _handlersManager.ToCallback(new AddChannelChatDirectlyCallbackData
        {
            ChannelId = data.ChannelId,
            Add = false,
            RequestFirstPost = true
        });
        var keyboard = new InlineKeyboardMarkup(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithUrl(ButtonsTextTemplate.AddBot,"https://t.me/zakup_robot?startgroup&admin=invite_users"),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Skip, skipData)
        });
            
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId, MessageTemplate.AddChannelChatText, replyMarkup:keyboard, cancellationToken: cancellationToken);
    }
}