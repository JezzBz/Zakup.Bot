using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.ChangeZakupPrice)]
public class ChangeZakupPriceCallbackHandler : ICallbackHandler<ChangePriceCallbackData>
{
    private readonly UserService _userService;
    private readonly HandlersManager _handlersManager;

    public ChangeZakupPriceCallbackHandler(UserService userService, HandlersManager handlersManager)
    {
        _userService = userService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, ChangePriceCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        state.Clear();
        state.State = UserStateType.ChangeZakupPrice;
        state.CachedValue = CacheHelper.ToCache(new ZakupIdCache
        {
            ZakupId = data.ZakupId
        });
        state.PreviousMessageId = callbackQuery.Message!.MessageId;
        await _userService.SetUserState(state, cancellationToken);

        var cancelData = await _handlersManager.ToCallback(new UpdateZakupCallbackData
        {
            ZakupId = data.ZakupId
        });

        var markUp = new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Cancel, cancelData)
        };

        await botClient.EditMessageTextAsync(
            callbackQuery.Message!.Chat.Id,
            callbackQuery.Message.MessageId,
            "Введите новую цену",
            replyMarkup: new InlineKeyboardMarkup(markUp),
            cancellationToken: cancellationToken);
    }
}