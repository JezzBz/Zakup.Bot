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
    private readonly MessagesService _messagesService;

    public ChannelAliasConfirmCallbackHandler(UserService userService, ChannelService channelService, HandlersManager handlersManager, MessagesService messagesService)
    {
        _userService = userService;
        _channelService = channelService;
        _handlersManager = handlersManager;
        _messagesService = messagesService;
    }

    public async Task Handle(ITelegramBotClient botClient, ConfirmChannelAlias data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUser(callbackQuery.From!.Id, cancellationToken);
        await botClient.SafeDelete(callbackQuery.From.Id, user.UserState.PreviousMessageId, cancellationToken);
        if (!data.Confirm)
        {
            var msg = await botClient.SendTextMessageAsync(
                callbackQuery.From.Id, 
                MessageTemplate.AddChannelAliasRequest, cancellationToken: cancellationToken);
            
            user.UserState!.CachedValue = CacheHelper.ToCache(new CreateChannelCacheData
            {
                ChannelId = data.ChannelId,
                RequestFirstPost = true
            });
            
            user.UserState.PreviousMessageId = msg.MessageId;
            
            await _userService.SetUserState(user.UserState, cancellationToken);
            return;
        }

        await _messagesService.SendMenu(botClient, user, cancellationToken);
        var channel = await _channelService.GetChannel(data.ChannelId, cancellationToken);
        channel!.Alias = data.Alias;
        await _channelService.UpdateChannel(channel, cancellationToken: cancellationToken);
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId, MessageTemplate.LabelChanged, cancellationToken: cancellationToken);

        if (channel.ChannelChatId == null)
        {
            var yesData = await _handlersManager.ToCallback(new AddChannelChatDirectlyCallbackData
            {
                ChannelId = channel.Id,
                Add = true,
                RequestFirstPost = false
            });
            
            var noData = await _handlersManager.ToCallback(new AddChannelChatDirectlyCallbackData
            {
                ChannelId = channel.Id,
                Add = false,
                RequestFirstPost = true
            });
            var keyboard = new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Yes, yesData),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.No, noData)
            });
            
            await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.AddChannelChatText, replyMarkup:keyboard, cancellationToken: cancellationToken);
        }
            
    }
    
   
}