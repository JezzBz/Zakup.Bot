using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Post;
using Zakup.Common.Enums;
using Zakup.Common.Models;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.ConfirmChannelAlias)]
public class ChannelAliasConfirmCallbackHandler :  ICallbackHandler<ConfirmChannelAlias>
{
    private readonly UserService _userService;
    private readonly ChannelService _channelService;
    private readonly HandlersManager _handlersManager;

    public ChannelAliasConfirmCallbackHandler(UserService userService, ChannelService channelService, HandlersManager handlersManager)
    {
        _userService = userService;
        _channelService = channelService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, ConfirmChannelAlias data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var state = await _userService.GetUserState(callbackQuery.From!.Id, cancellationToken);
        await botClient.SafeDelete(callbackQuery.From.Id, state.PreviousMessageId, cancellationToken);
        if (!data.Confirm)
        {
            var msg = await botClient.SendTextMessageAsync(
                callbackQuery.From.Id, 
                MessageTemplate.AddChannelAliasRequest, cancellationToken: cancellationToken);
            
            state!.CachedValue = CacheHelper.ToCache(new CreateChannelCacheData
            {
                ChannelId = data.ChannelId
            });
            
            state.PreviousMessageId = msg.MessageId;
            
            await _userService.SetUserState(state, cancellationToken);
            return;
        }
        
        var channel = await _channelService.GetChannel(data.ChannelId, cancellationToken);
        channel.Alias = data.Alias;
        await _channelService.UpdateChannel(channel, cancellationToken: cancellationToken);
        await SendFirstPostMessage(botClient, state.UserId, data.ChannelId);
        
    }

    public ConfirmChannelAlias Parse(List<string> parameters)
    {

        if (parameters.Count < 2)
        {
            throw new Exception();
        }
        
        return new ConfirmChannelAlias
        {
            Alias = parameters[0],
            ChannelId = long.Parse(parameters[1]),
            Confirm = bool.Parse(parameters[2])
        };
    }

    private async Task SendFirstPostMessage(ITelegramBotClient botClient, long userId, long channelId)
    {
        var yesData = _handlersManager.ToCallback(new FirstAdPostCallbackData
        {
            ChannelId = channelId,
            Create = true
        });
        
        var noData = _handlersManager.ToCallback(new FirstAdPostCallbackData
        {
            ChannelId = channelId,
            Create = false
        });
        
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Yes, yesData),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Later, noData)
            }
        });

        await botClient.SendTextMessageAsync(
            userId,
            MessageTemplate.CreateFirstPostRequest,
            replyMarkup: keyboard
        );
    }
}