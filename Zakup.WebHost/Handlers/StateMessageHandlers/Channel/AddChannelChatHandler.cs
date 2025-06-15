using Mono.TextTemplating;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.Channel;

[StateType(UserStateType.AddChannelChat)]
public class AddChannelChatHandler : IStateHandler
{
    private readonly UserService _userService;
    private readonly ChannelService _channelService;
    private readonly MessagesService _messagesService;

    public AddChannelChatHandler(UserService userService, ChannelService channelService, MessagesService messagesService)
    {
        _userService = userService;
        _channelService = channelService;
        _messagesService = messagesService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        var userId = update.Message.From.Id;
        var user = await _userService.GetUser(userId, cancellationToken);
        var channelId = CacheHelper.ToData<ChannelIdCache>(user.UserState.CachedValue).ChannelId;
        if (message.ForwardFromChat is null)
        {
            await botClient.SendTextMessageAsync(userId, MessageTemplate.ForwardWhenAdChatError, cancellationToken: cancellationToken);
            return;
        }

        IEnumerable<ChatMember> admins;

        try
        {
            admins = await botClient.GetChatAdministratorsAsync(message.ForwardFromChat.Id, cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex) when (ex.Message.Contains("bot is not a member"))
        {
            await botClient.SendTextMessageAsync(userId, MessageTemplate.ForwardWhenAdChatBotError, cancellationToken: cancellationToken);
            return;
        }

        var chat = await botClient.GetChatAsync(message.ForwardFromChat, cancellationToken: cancellationToken);
        if (chat.LinkedChatId != channelId)
        {
            await botClient.SendTextMessageAsync(userId, MessageTemplate.ChatNotConnectedToChannelError, cancellationToken: cancellationToken);
            return;
        }

        var me = await botClient.GetMeAsync(cancellationToken: cancellationToken);

        if (!admins.Any(x => x.User.IsBot && x.User.Id == me.Id))
        {
            await botClient.SendTextMessageAsync(userId, MessageTemplate.BotNotAdminInChatError, cancellationToken: cancellationToken);
            return;
        }

        await _channelService.AddChannelChat(channelId, message.ForwardFromChat.Id, cancellationToken);
        var msg = await botClient.SendTextMessageAsync(userId,MessageTemplate.Success , cancellationToken: cancellationToken);
        await _messagesService.SendMenu(botClient, user, cancellationToken, menuMessageId: user.UserState.PreviousMessageId);
    }
}