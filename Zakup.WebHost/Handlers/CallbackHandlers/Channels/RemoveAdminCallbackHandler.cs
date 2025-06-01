using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.RemoveAdmin)]
public class RemoveAdminCallbackHandler : ICallbackHandler<RemoveAdminCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly MessagesService _messagesService;
    private readonly UserService _userService;

    public RemoveAdminCallbackHandler(ChannelService channelService, MessagesService messagesService, UserService userService)
    {
        _channelService = channelService;
        _messagesService = messagesService;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, RemoveAdminCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetUser(callbackQuery.From.Id, cancellationToken);
        await _channelService.RemoveAdmin(data.ChannelId, data.AdminUserId, cancellationToken);
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message.MessageId, MessageTemplate.AdminRemoved, cancellationToken: cancellationToken);
        await _messagesService.SendMenu(botClient, user,cancellationToken);
    }

    public RemoveAdminCallbackData Parse(List<string> parameters)
    {
        return new RemoveAdminCallbackData
        {
            ChannelId = long.Parse(parameters[0]),
            AdminUserId = long.Parse(parameters[1])
        };
    }
}