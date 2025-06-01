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

[CallbackType(CallbackType.DisableAutoApprove)]
public class DisableAutoApproveCallbackHandler : ICallbackHandler<DisableAutoApproveCallbackData>
{
    private readonly ChannelService _channelService;
    private readonly MessagesService _messagesService;
    private readonly UserService _userService;

    public DisableAutoApproveCallbackHandler(ChannelService channelService, MessagesService messagesService, UserService userService)
    {
        _channelService = channelService;
        _messagesService = messagesService;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, DisableAutoApproveCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var channel = await _channelService.GetChannel(data.ChannelId, cancellationToken);
        if (channel is null)
        {
            await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId,
                MessageTemplate.ChannelNotFound, cancellationToken: cancellationToken);
            return;
        }
        
        channel.MinutesToAcceptRequest = null;
        await _channelService.UpdateChannel(channel, cancellationToken);
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId, MessageTemplate.AutoApproveWasDisabled, cancellationToken: cancellationToken);
        var user = await _userService.GetUser(callbackQuery.From.Id, cancellationToken);
        await _messagesService.SendMenu(botClient, user, cancellationToken);
    }

    public DisableAutoApproveCallbackData Parse(List<string> parameters)
    {
        return new DisableAutoApproveCallbackData()
        {
            ChannelId = long.Parse(parameters[0]),
        };
    }
}