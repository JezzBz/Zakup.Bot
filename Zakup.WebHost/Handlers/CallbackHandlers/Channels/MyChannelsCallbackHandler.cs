using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.MyChannelsList)]
public class MyChannelsCallbackHandler : ICallbackHandler<EmptyCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly UserService _userService;
    private readonly HandlersManager _handlersManager;

    public MyChannelsCallbackHandler(ChannelService channelService, UserService userService, HandlersManager handlersManager)
    {
        _channelService = channelService;
        _userService = userService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userState = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        var channels = await _channelService.GetChannels(callbackQuery.From.Id, cancellationToken);
        var buttons = new List<List<InlineKeyboardButton>>();

        for (int i = 0; i < channels.Count; i += 2)
        {
            var buttonData = await _handlersManager.ToCallback(new ShowChannelMenuCallbackData
            {
                ChannelId = channels[i].Id
            });
            
            var row = new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData($"[{channels[i].Alias}] {channels[i].Title}", buttonData)
            };
            if (i + 1 < channels.Count)
            {
                row.Add(InlineKeyboardButton.WithCallbackData($"[{channels[i + 1].Alias}] {channels[i + 1].Title}", buttonData));
            }
            buttons.Add(row);
        }

        buttons.Add([
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.NewChannel, nameof(CallbackType.NewChannel)),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Back, $"menu|{callbackQuery.Message!.MessageId}")
        ]);
        
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId, MessageTemplate.ChannelList,
            replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: cancellationToken);
        userState.MenuMessageId = null;
        
        await _userService.SetUserState(userState.UserId, userState, cancellationToken);
    }
}