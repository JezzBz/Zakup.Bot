using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.StateMessageHandlers.Zakup;

[StateType(UserStateType.ChooseZakupDate)]
public class ZakupAddDateHandler : IStateHandler
{
    private readonly UserService _userService;
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;

    public ZakupAddDateHandler(UserService userService, ZakupService zakupService, HandlersManager handlersManager)
    {
        _userService = userService;
        _zakupService = zakupService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        if (!DateTime.TryParseExact(message.Text.Trim(), "dd.MM.yyyy HH:mm", null,
                DateTimeStyles.None, out var postDateTime))
        {
            await botClient.SendTextMessageAsync(message.Chat.Id,
                MessageTemplate.BadZakupDateError, cancellationToken: cancellationToken);
            return;
        }
        
        var state = await _userService.GetUserState(update.Message!.From.Id, cancellationToken);
        var zakupId = CacheHelper.ToData<ZakupIdCache>(state.CachedValue).ZakupId;
        var zakup = await _zakupService.Get(zakupId, cancellationToken);
        zakup.PostTime = postDateTime.ToUniversalTime();
        await _zakupService.Update(zakup, cancellationToken);

        state.State = UserStateType.AddZakupPrice;
        await _userService.SetUserState(state, cancellationToken);
        var cancelData = _handlersManager.ToCallback(new DeleteZakupCallbackData
        {
            ZakupId = zakupId
        });
        
        var freeData = _handlersManager.ToCallback(new ZakupCheckChannelPrivateCallbackData()
        {
            ZakupId = zakupId
        });
        var keyBoard = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Cancel, cancelData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Free, freeData),
        };
        
        await botClient.SendTextMessageAsync(state.UserId, MessageTemplate.ZakupPrice, replyMarkup: new InlineKeyboardMarkup(keyBoard), cancellationToken: cancellationToken);
    }
}