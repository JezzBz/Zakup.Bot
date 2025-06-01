using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.Channel;

[StateType(UserStateType.SetApproveTime)]
public class SetAutoApproveTimeHandler : IStateHandler
{
    private readonly UserService _userService;
    private readonly ChannelService _channelService;
    private readonly MessagesService _messagesService;

    public SetAutoApproveTimeHandler(UserService userService, ChannelService channelService, MessagesService messagesService)
    {
        _userService = userService;
        _channelService = channelService;
        _messagesService = messagesService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var parsed = long.TryParse(update.Message?.Text, out var time);
        if (!parsed)
        {
            await botClient.SendTextMessageAsync(update.Message!.From!.Id, MessageTemplate.InvalidApproveTimeError,
                cancellationToken: cancellationToken);
            return;
        }
        var user = await _userService.GetUser(update.Message!.From!.Id, cancellationToken);
        
        var cache = CacheHelper.ToData<SetApproveTimeCache>(user!.UserState!.CachedValue!);
        
        var channel = await _channelService.GetChannel(cache!.ChannelId, cancellationToken);

        if (channel == null)
        {
            await botClient.SendTextMessageAsync(update.Message.From.Id, MessageTemplate.ChannelNotFound, cancellationToken: cancellationToken);
            return;
        }

        channel.MinutesToAcceptRequest = time;
        await _channelService.UpdateChannel(channel, cancellationToken);
        await botClient.SendTextMessageAsync(update.Message.From.Id, MessageTemplate.AutoApproveEnabled(time), cancellationToken: cancellationToken);
        await _messagesService.SendMenu(botClient, user, cancellationToken);
    }
}