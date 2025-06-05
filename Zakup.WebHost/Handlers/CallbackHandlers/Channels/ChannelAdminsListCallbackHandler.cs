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

[CallbackType(CallbackType.AddChannelAdmin)]
public class ChannelAdminsListCallbackHandler : ICallbackHandler<ChannelAdminsListCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly HandlersManager _handlersManager;

    public ChannelAdminsListCallbackHandler(ChannelService channelService, HandlersManager handlersManager)
    {
        _channelService = channelService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, ChannelAdminsListCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var channel = await _channelService.GetChannel(data.ChannelId, cancellationToken);
        if (channel == null)
        {
            await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.ChannelNotFound, cancellationToken: cancellationToken);
            return;
        }
        var admins = await _channelService.GetAdmins(data.ChannelId, cancellationToken);
        
        var buttons = new List<List<InlineKeyboardButton>>();
        foreach (var admin in channel.Administrators)
        {
            var displayName = string.IsNullOrEmpty(admin.UserName) ? admin.Id.ToString() : "@" + admin.UserName;
            var cbqData = await _handlersManager.ToCallback(new AdminInfoCallbackData
            {
                UserId = admin.Id,
                ChannelId = data.ChannelId
            });
            
            buttons.Add([InlineKeyboardButton.WithCallbackData(displayName, cbqData)]);
        }

        var newAdminData = await _handlersManager.ToCallback(new AddNewAdminCallbackData
        {
            ChannelId = data.ChannelId
        });
        
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.AddNewAdmin, newAdminData)
        });

        var backData = await _handlersManager.ToCallback(new ShowChannelMenuCallbackData
        {
            ChannelId = data.ChannelId
        });
        
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, backData)
        });

        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId, MessageTemplate.ChannelAdmins,
            replyMarkup: new InlineKeyboardMarkup(buttons),  cancellationToken: cancellationToken);
    }
}