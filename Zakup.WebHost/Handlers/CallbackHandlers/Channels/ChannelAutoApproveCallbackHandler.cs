using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.ChannelAutoApprove)]
public class ChannelAutoApproveCallbackHandler : ICallbackHandler<ChannelAutoApproveCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly HandlersManager _handlersManager;

    public ChannelAutoApproveCallbackHandler(ChannelService channelService, HandlersManager handlersManager)
    {
        _channelService = channelService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, ChannelAutoApproveCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var channel = await _channelService.GetChannel(data.ChannelId, cancellationToken);
        
        if (channel == null)
        {
            await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.ChannelNotFound, cancellationToken: cancellationToken);
            return;
        }

        var isActuallyEnabled = channel.MinutesToAcceptRequest.HasValue;
        var keyBoard = await GetKeyboard(isActuallyEnabled, data.ChannelId);
        var statusText = isActuallyEnabled
            ? MessageTemplate.AutoApproveEnabled(channel.MinutesToAcceptRequest!.Value)
            : MessageTemplate.AutoApproveDisabled;
        
        var messageText = CommandsHelper.EscapeMarkdownV2(statusText);

        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId, messageText,
            ParseMode.MarkdownV2, replyMarkup: keyBoard, cancellationToken: cancellationToken);
    }

    private async Task<InlineKeyboardMarkup> GetKeyboard(bool isEnabled, long channelId)
    {
        InlineKeyboardButton button;
        var backData = await _handlersManager.ToCallback(new ShowChannelMenuCallbackData
        {
            ChannelId = channelId
        });
        
        if (isEnabled)
        {
            var callbackData = await _handlersManager.ToCallback(new DisableAutoApproveCallbackData
            {
                ChannelId = channelId
            });
            
            button = InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.DisableAutoApprove, callbackData);
        }
        else
        {
            var callbackData = await _handlersManager.ToCallback(new EnableAutoApproveCallbackData
            {
                ChannelId = channelId
            });
            
            button = InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.EnableAutoApprove, callbackData);
        }
        
        return new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                button
            },
            new List<InlineKeyboardButton>
            {
               InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, backData)
            }
        });
    }
}