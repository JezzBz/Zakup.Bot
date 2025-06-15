using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.StateMessageHandlers.Admin;

[StateType(UserStateType.AdminUnscam)]
public class UnScamAdminHandler : IStateHandler
{
    private readonly MessagesService _messagesService;
    private readonly ChannelService _channelService;
    private readonly UserService _userService;

    public UnScamAdminHandler(ChannelService channelService, MessagesService messagesService, UserService userService)
    {
        _channelService = channelService;
        _messagesService = messagesService;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        
        if (string.IsNullOrEmpty(message.Text))
            throw new InvalidOperationException();
        if (!long.TryParse(message.Text, out var channelId))
        {
            if (message.ForwardFromChat is null)
                throw new InvalidOperationException();
            channelId = message.ForwardFromChat.Id;
        }
        
        await _channelService.AddRating(new ChannelRating
        {
            ChannelId = channelId,
            BadDeals = -1
        }, cancellationToken);
        
        var adminUser = await _userService.GetUser(message.From.Id,cancellationToken);
        await botClient.SafeEdit(adminUser.Id, adminUser.UserState.PreviousMessageId, MessageTemplate.ChannelScamRemoved, cancellationToken: cancellationToken);
        await _messagesService.SendMenu(botClient, adminUser, cancellationToken);
    }
}