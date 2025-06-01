using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.StateMessageHandlers.Zakup;

[StateType(UserStateType.ZakupChannelAlias)]
public class ZakupChannelAliasHandler : IStateHandler
{
    private readonly UserService _userService;
    private readonly ZakupService _zakupService;

    public ZakupChannelAliasHandler(UserService userService, ZakupService zakupService)
    {
        _userService = userService;
        _zakupService = zakupService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
       
        var message = update.Message;
        
        var state = await _userService.GetUserState(update.Message.From.Id, cancellationToken);
        await botClient.SafeDelete(state.UserId, state.PreviousMessageId,cancellationToken);
        var zakupId = CacheHelper.ToData<ZakupIdCache>(state.CachedValue).ZakupId;
        var zakup = await _zakupService.Get(zakupId, cancellationToken);
        if (message.ForwardFromChat != null)
        {
            zakup.Platform = message.ForwardFromChat.Title;
        }
        else if (!string.IsNullOrWhiteSpace(message.Text) && message.Text.Length <= 60)
        {
            zakup.Platform = message.Text;
        }
        else
        {
            await botClient.SendTextMessageAsync(state.UserId, MessageTemplate.BadZakupChannelAlias, cancellationToken: cancellationToken);
            return;
        }

        await _zakupService.Update(zakup, cancellationToken);
        
        var optionsMarkup = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.FinalAdPost, $"replace:{zakupId}"),
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.OnlyLink, $"get_link:{zakupId}")
            },
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.PremiumEmoji, $"premium:{zakupId}")
            }
        });
        await botClient.SafeDelete(state.UserId, message.MessageId, cancellationToken);
        await botClient.SafeEdit(state.UserId, state.PreviousMessageId,MessageTemplate.ZakupActions ,replyMarkup: optionsMarkup, cancellationToken:cancellationToken);
    }
}