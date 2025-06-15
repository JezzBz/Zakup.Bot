using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Admin;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Admin;

[CallbackType(CallbackType.AdminScamConfirm)]
public class AdminConfirmScamCallbackHandler : ICallbackHandler<AdminConfirmScamCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly MessagesService _messagesService;
    private readonly UserService _userService;
    
    public AdminConfirmScamCallbackHandler(ChannelService channelService, MessagesService messagesService, UserService userService)
    {
        _channelService = channelService;
        _messagesService = messagesService;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, AdminConfirmScamCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        await _channelService.AddRating(new ChannelRating
        {
            ChannelId = data.ChannelId,
            BadDeals = 1
        }, cancellationToken);

        var user = await _userService.GetUser(callbackQuery.From.Id, cancellationToken);
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId,MessageTemplate.MarkedAsScam, cancellationToken: cancellationToken);
        await _messagesService.SendMenu(botClient, user, cancellationToken);
    }
}