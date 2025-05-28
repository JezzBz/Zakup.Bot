using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Common.Models;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.Channel;

[StateType(UserStateType.CreateChannelAlias)]
public class CreateChannelAliasHandler : IStateHandler
{
    private readonly ChannelService _channelService;
    private readonly UserService _userService;
    private readonly HandlersManager _handlersManager;
    public CreateChannelAliasHandler(ChannelService channelService, UserService userService, HandlersManager handlersManager)
    {
        _channelService = channelService;
        _userService = userService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var alias = update.Message?.Text;
        
        if (string.IsNullOrEmpty(alias) || alias.Contains(" ") || alias.Length > 15)
        {
            var userId = update.Message?.From?.Id ?? update.CallbackQuery?.From.Id;
            await botClient.SendTextMessageAsync(userId!, MessageTemplate.ChannelAliasStateNotification, cancellationToken: cancellationToken);
            return;
        }

        var state = await _userService.GetUserState(update.Message!.From!.Id, cancellationToken);
        await botClient.SafeDelete(state!.UserId, state.PreviousMessageId, cancellationToken);
        var data = CacheHelper.ToData<CreateChannelCacheData>(state!.CachedValue!);
        var channel = await _channelService.GetChannel(data!.ChannelId);
            
        
        alias = alias.ToLowerInvariant();
        var callbackData = _handlersManager.ToCallback(new ConfirmChannelAlias
        {
            Alias = alias,
            ChannelId = data!.ChannelId,
            Confirm = true
        });
        var emptyData = _handlersManager.ToCallback(new ConfirmChannelAlias
        {
            ChannelId = data!.ChannelId,
            Confirm = false
        });
        var msg = await botClient.SendTextMessageAsync(update.Message?.From?.Id, $"Для вашего канала «{channel.Title}» будет установлена метка: {alias}. Вы уверены?"
            , replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Yes, callbackData), 
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.No, emptyData)
            }), cancellationToken: cancellationToken);
        state.PreviousMessageId = msg.MessageId;
        await _userService.SetUserState(state.UserId, state, cancellationToken);
    }
}