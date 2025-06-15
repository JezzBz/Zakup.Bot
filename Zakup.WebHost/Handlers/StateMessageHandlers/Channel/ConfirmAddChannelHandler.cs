using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Abstractions.Handlers;
using Zakup.Common.Enums;
using Zakup.Common.Models;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.Channel;

[StateType(UserStateType.ConfirmAddChannel)]
public class ConfirmAddChannelHandler : IStateHandler
{
    private readonly ChannelService _channelService;
    private readonly UserService _userService;
    private readonly InternalSheetsService _sheetsService;

    public ConfirmAddChannelHandler(ChannelService channelService, UserService userService, InternalSheetsService sheetsService)
    {
        _channelService = channelService;
        _userService = userService;
        _sheetsService = sheetsService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
       
       var message = update.Message;
       var state = await _userService.GetUserState(message.From.Id, cancellationToken);
       await ValidateMessage(message, botClient, cancellationToken);
       var meBot = await botClient.GetMeAsync(cancellationToken: cancellationToken);
       var admins = await GetAdmins(botClient, message!.ForwardFromChat!, message!.From!.Id, meBot, cancellationToken);
       if (admins is null)
       {
           return;
       }

       var existChannel = await _channelService.GetChannel(message!.ForwardFromChat!.Id, cancellationToken);
       
       // Если канал ранее удаляли – «воскрешаем» его
       if (existChannel?.HasDeleted == true)
       {
           await RestoreChannel(botClient, message, cancellationToken);
           return;
       }
       
       existChannel = await CreateOrUpdateChannel(admins, message, existChannel, cancellationToken);
      
       // Если канал уже существовал, проверяем Alias
       await CheckChannelAlias(botClient, existChannel, message.From.Id, message, cancellationToken);
    }


    private async Task ValidateMessage(Message? message, ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (message!.ForwardFromChat is null || message.ForwardFromChat.Type != ChatType.Channel)
        {
            Console.WriteLine("Message is not a forwarded message from a channel.");
            await botClient.SendTextMessageAsync(
                message.From!.Id,
                MessageTemplate.ForwardPostFromChannelRequest, cancellationToken: cancellationToken);
            return;
        }

        // Если переслал бот, просим именно от канала
        if (message.ForwardFrom is not null && message.ForwardFrom.IsBot)
        {
            Console.WriteLine("Message was forwarded from a bot.");
            await botClient.SendTextMessageAsync(
                message.From!.Id,
                MessageTemplate.MessageForwardedFromBotError, cancellationToken: cancellationToken);
            return;
        }
    }

    private async Task<IEnumerable<ChatMember>?> GetAdmins(ITelegramBotClient botClient, Chat channelChat, long userId, User me, CancellationToken cancellationToken)
    {
        IEnumerable<ChatMember> admins;
        try
        {
            admins = await botClient.GetChatAdministratorsAsync(channelChat.Id, cancellationToken: cancellationToken);
            Console.WriteLine("Fetched chat administrators successfully.");
        }
        catch (ApiRequestException ex) when (
            ex.Message.Contains("bot is not a member of the channel chat") ||
            ex.Message.Contains("member list is inaccessible"))
        {
            Console.WriteLine($"Ошибка получения администраторов канала: {ex.Message}");
            await botClient.SendTextMessageAsync(
                userId,
                MessageTemplate.BotIsNotMemberError, cancellationToken: cancellationToken);
            return null;
        }

        if (!admins.Any(x => x.User.IsBot && x.User.Id == me.Id))
        {
            Console.WriteLine("Bot is not an administrator in the channel.");
            await botClient.SendTextMessageAsync(
                userId,
                MessageTemplate.BotIsNotAdminError, cancellationToken: cancellationToken);
            return null;
        }

        return admins;
    }

    private async Task<TelegramChannel> RestoreChannel(ITelegramBotClient botClient, Message? message,  CancellationToken cancellationToken)
    {
        var userState =  await _userService.GetUserState(message.From!.Id, cancellationToken);
        var existChannel = await _channelService.ActivateRemovedChannel(message!.From!.Id, message!.ForwardFromChat!.Id);
        if (!await _sheetsService.CheckIfSheetExists(userState.UserId, existChannel.Id))
            await _sheetsService.CreateSheet(existChannel.Id, existChannel.Title, message.From?.Username ?? "stat", userState.UserId);
        
        var msgText = !string.IsNullOrEmpty(existChannel.Alias)
            ?  MessageTemplate.ChannelRestoredMessage(existChannel.Alias)
            : MessageTemplate.ChannelRestoredNeedAlias;
        
        var msg = await botClient.SendTextMessageAsync(message.From.Id, msgText, cancellationToken: cancellationToken);
        
        if (string.IsNullOrEmpty(existChannel.Alias))
        {
            await ToCreateAliasState(message, existChannel, msg.MessageId,userState, cancellationToken);
        }
    
        return existChannel;
    }

    private async Task<TelegramChannel> CreateOrUpdateChannel(IEnumerable<ChatMember> admins, Message? message, TelegramChannel existChannel, CancellationToken cancellationToken)
    {
        var adminsList = await _userService.GetOrCreateAdminUsers(admins, cancellationToken);
     
        var channel = await _channelService.CreateOrUpdateChannel(message!.ForwardFromChat!, adminsList, existChannel, cancellationToken);
        
         await _sheetsService.CreateSheet(
             channel.Id,
             channel.Title,
             message.From?.Username ?? "stat",
             message.From.Id
         );
        return channel;
    }

    private async Task CheckChannelAlias(ITelegramBotClient botClient, TelegramChannel existChannel, long userId, Message message, CancellationToken cancellationToken)
    {
        var userState =  await _userService.GetUserState(message.From!.Id, cancellationToken);
        
        // Если канал уже существовал, проверяем Alias
        if (existChannel != null && !string.IsNullOrEmpty(existChannel.Alias))
        {
            var msg = await botClient.SendTextMessageAsync(
                userId,
                MessageTemplate.ChannelCreatedMessage(existChannel.Alias), cancellationToken: cancellationToken);
            userState!.Clear();
            await _userService.SetUserState(userId, userState, cancellationToken);
        }
        else
        {
            var msg = await botClient.SendTextMessageAsync(
                userId,
                MessageTemplate.AddChannelAliasRequest, cancellationToken: cancellationToken);

            await ToCreateAliasState(message, existChannel, msg.MessageId, userState, cancellationToken);
        }
    }


    private async Task ToCreateAliasState(Message message, TelegramChannel existChannel, int previousMessageId, TelegramUserState state, CancellationToken cancellationToken)
    {
        var currentState = state;
        currentState!.State = UserStateType.CreateChannelAlias;
        currentState.CachedValue = CacheHelper.ToCache(new CreateChannelCacheData
        {
            ChannelId = existChannel.Id,
            RequestFirstPost = true
        });
        currentState.PreviousMessageId = previousMessageId;
        await _userService.SetUserState(message.From!.Id, currentState, cancellationToken);
    }
    
}