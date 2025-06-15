using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Common.Models;
using Zakup.Entities;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers;

[CallbackType(CallbackType.AddChannelDirectly)]
public class AddChannelDirectlyCallbackHandler : ICallbackHandler<AddChannelDirectlyCallbackData>
{
    private readonly UserService _userService;
    private readonly InternalSheetsService _sheetsService;
    private readonly ChannelService _channelService;

    public AddChannelDirectlyCallbackHandler(UserService userService, InternalSheetsService sheetsService, ChannelService channelService)
    {
        _userService = userService;
        _sheetsService = sheetsService;
        _channelService = channelService;
    }

    public async Task Handle(ITelegramBotClient botClient, AddChannelDirectlyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var meBot = await botClient.GetMeAsync(cancellationToken: cancellationToken);
        var admins = await GetAdmins(botClient, data.ChannelId,callbackQuery.From!.Id, meBot, cancellationToken);
        if (admins is null)
        {
            return;
        }

        var existChannel = await _channelService.GetChannel(data.ChannelId, cancellationToken);
       
        // Если канал ранее удаляли – «воскрешаем» его
        if (existChannel?.HasDeleted == true)
        {
            await RestoreChannel(botClient, callbackQuery.From, data.ChannelId, cancellationToken);
            return;
        }
       
        existChannel = await CreateOrUpdateChannel(admins,callbackQuery.From, data.ChannelId, data.ChannelTitle, existChannel, cancellationToken);
      
        // Если канал уже существовал, проверяем Alias
        await CheckChannelAlias(botClient, existChannel, callbackQuery.From.Id, cancellationToken);
    }
    
     private async Task<IEnumerable<ChatMember>?> GetAdmins(ITelegramBotClient botClient, long channelId, long userId, User me, CancellationToken cancellationToken)
    {
        IEnumerable<ChatMember> admins;
        try
        {
            admins = await botClient.GetChatAdministratorsAsync(channelId, cancellationToken: cancellationToken);
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

    private async Task<TelegramChannel> RestoreChannel(ITelegramBotClient botClient, User fromUser, long channelId,  CancellationToken cancellationToken)
    {
        var userState =  await _userService.GetUserState(fromUser.Id, cancellationToken);
        var existChannel = await _channelService.ActivateRemovedChannel(fromUser.Id, channelId);
        if (!await _sheetsService.CheckIfSheetExists(userState.UserId, existChannel.Id))
            await _sheetsService.CreateSheet(existChannel.Id, existChannel.Title, fromUser.Username ?? "stat", userState.UserId);
        
        var msgText = !string.IsNullOrEmpty(existChannel.Alias)
            ?  MessageTemplate.ChannelRestoredMessage(existChannel.Alias)
            : MessageTemplate.ChannelRestoredNeedAlias;
        
        var msg = await botClient.SendTextMessageAsync(fromUser.Id, msgText, cancellationToken: cancellationToken);
        
        if (string.IsNullOrEmpty(existChannel.Alias))
        {
            await ToCreateAliasState(fromUser.Id, existChannel, msg.MessageId,userState, cancellationToken);
        }
    
        return existChannel;
    }

    private async Task<TelegramChannel> CreateOrUpdateChannel(IEnumerable<ChatMember> admins, User fromUser, long channelId, string channelTitle ,TelegramChannel existChannel, CancellationToken cancellationToken)
    {
        var adminsList = await _userService.GetOrCreateAdminUsers(admins, cancellationToken);
     
        var channel = await _channelService.CreateOrUpdateChannel(channelId, channelTitle, adminsList, existChannel, cancellationToken);
        
         await _sheetsService.CreateSheet(
             channel.Id,
             channel.Title,
             fromUser.Username ?? "stat",
             fromUser.Id
         );
        return channel;
    }

    private async Task CheckChannelAlias(ITelegramBotClient botClient, TelegramChannel existChannel, long userId, CancellationToken cancellationToken)
    {
        var userState =  await _userService.GetUserState(userId, cancellationToken);
        
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

            await ToCreateAliasState(userId, existChannel, msg.MessageId, userState, cancellationToken);
        }
    }


    private async Task ToCreateAliasState(long fromUserId ,TelegramChannel existChannel, int previousMessageId, TelegramUserState state, CancellationToken cancellationToken)
    {
        var currentState = state;
        currentState!.State = UserStateType.CreateChannelAlias;
        currentState.CachedValue = CacheHelper.ToCache(new CreateChannelCacheData
        {
            ChannelId = existChannel.Id,
            RequestFirstPost = true
        });
        currentState.PreviousMessageId = previousMessageId;
        await _userService.SetUserState(fromUserId, currentState, cancellationToken);
    }
}