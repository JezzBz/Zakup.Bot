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

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.ZakupChannelPrivate)]
public class ZakupCheckChannelPrivateCallbackHandler : ICallbackHandler<ZakupCheckChannelPrivateCallbackData>
{
    private readonly ZakupService _zakupService;
    private readonly ChannelService _channelService;
    private readonly HandlersManager _handlersManager;
    private readonly UserService _userService;

    public ZakupCheckChannelPrivateCallbackHandler(ZakupService zakupService, ChannelService channelService, HandlersManager handlersManager, UserService userService)
    {
        _zakupService = zakupService;
        _channelService = channelService;
        _handlersManager = handlersManager;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, ZakupCheckChannelPrivateCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var zakup = await _zakupService.Get(data.ZakupId, cancellationToken:cancellationToken);
        var isPublicChannel = await _channelService.IsPublic(zakup.ChannelId, botClient, cancellationToken);
        var messageText = string.Empty;
        InlineKeyboardMarkup optionsMarkup;
        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        state.Clear();
        await _userService.SetUserState(state, cancellationToken);
        var publicData = await _handlersManager.ToCallback(new ZakupLinkTypeCallbackData
        {
            ZakupId = data.ZakupId,
            Private = true
        });
        
        
        if (isPublicChannel)
        {
            messageText = MessageTemplate.PublicChannelNotification;
            optionsMarkup = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Continue, publicData)
                }
            });
        }
        else
        {
            var privateData = await _handlersManager.ToCallback(new ZakupLinkTypeCallbackData
            {
                ZakupId = data.ZakupId,
                Private = true
            });
            optionsMarkup = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.WithRequests, privateData),
                    InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Opened, publicData)
                }
            });

            messageText = MessageTemplate.ChooseLinkType;
        }
        
        await botClient.SafeEdit(
            chatId: callbackQuery.From.Id, 
            messageId: callbackQuery.Message.MessageId, 
            text: messageText, 
            replyMarkup: optionsMarkup, cancellationToken: cancellationToken);
    }

    public ZakupCheckChannelPrivateCallbackData Parse(List<string> parameters)
    {
        return new ZakupCheckChannelPrivateCallbackData
        {
            ZakupId = Guid.Parse(parameters[0]),
        };
    }
}