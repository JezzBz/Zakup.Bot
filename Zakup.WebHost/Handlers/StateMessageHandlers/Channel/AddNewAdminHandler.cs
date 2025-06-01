using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.Channel;

[StateType(UserStateType.AddAdmin)]
public class AddNewAdminHandler : IStateHandler
{
    private readonly UserService _userService;
    private readonly ChannelService _channelService;
    
    
    public AddNewAdminHandler(UserService userService, ChannelService channelService)
    {
        _userService = userService;
        _channelService = channelService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        await ValidateMessage(message, botClient, cancellationToken);
        var state = await _userService.GetUserState(update.Message.From.Id, cancellationToken);
        var channelId = CacheHelper.ToData<ChannelIdCache>(state.CachedValue).ChannelId;
        var potentialAdminId = message.ForwardFrom.Id;
        var potentialAdminUser = await _userService.GetUser(potentialAdminId, cancellationToken);
        if (potentialAdminUser == null)
        {
            potentialAdminUser = await _userService.CreateUser(potentialAdminId, message.ForwardFrom.Username,
                cancellationToken: cancellationToken);
        }

        var channel = await _channelService.GetChannel(channelId, cancellationToken);
        if (channel == null)
        {
            await botClient.SendTextMessageAsync(update.Message.From.Id, MessageTemplate.ChannelNotFound, cancellationToken: cancellationToken);
            return;
        }
        var isAdmin = await _channelService.IsAdmin(channelId, potentialAdminId, cancellationToken);

        if (isAdmin)
        {
            await botClient.SendTextMessageAsync(message.From!.Id, MessageTemplate.IsAlreadyAdmin, cancellationToken: cancellationToken);
            return;
        }

        await _channelService.MakeAdmin(channelId, potentialAdminId, cancellationToken);
        
        await botClient.SendTextMessageAsync(message.From.Id, MessageTemplate.AdminCreated, cancellationToken: cancellationToken);
        //TODO: добавить таблицы
    }
    
    private async Task ValidateMessage(Message? message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (message.ForwardFrom is null)
        {
            await botClient.SendTextMessageAsync(
                message.From!.Id,
                MessageTemplate.AdminForwardEmptyError, cancellationToken: cancellationToken);
            return;
        }
    }
}