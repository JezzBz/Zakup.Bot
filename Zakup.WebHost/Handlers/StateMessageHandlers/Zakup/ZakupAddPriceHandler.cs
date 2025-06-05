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

[StateType(UserStateType.AddZakupPrice)]
public class ZakupAddPriceHandler : IStateHandler
{
    private readonly ZakupService _zakupService;
    private readonly UserService _userService;
    private readonly HandlersManager _handlersManager;

    public ZakupAddPriceHandler(UserService userService, HandlersManager handlersManager, ZakupService zakupService)
    {
        _userService = userService;
        _handlersManager = handlersManager;
        _zakupService = zakupService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (!decimal.TryParse(update.Message.Text, out var amount))
        {
            await botClient.SendTextMessageAsync(update.Message.From!.Id, MessageTemplate.ChooseZakupDate,
                cancellationToken: cancellationToken);
            return;
        }
        var state = await _userService.GetUserState(update.Message!.From.Id, cancellationToken);
        var zakupId = CacheHelper.ToData<ZakupIdCache>(state.CachedValue).ZakupId;
        var zakup = await _zakupService.Get(zakupId, cancellationToken:cancellationToken);
        zakup.Price = amount;
        await _zakupService.Update(zakup, cancellationToken);
        state.Clear();
        await _userService.SetUserState(state, cancellationToken);
        var callbackData = await _handlersManager.ToCallback(new ZakupCheckChannelPrivateCallbackData()
        {
            ZakupId = zakupId
        });
        
        var keyboard = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Continue, callbackData)
        };
        
        await botClient.SendTextMessageAsync(update.Message.From!.Id, 
            MessageTemplate.PriceSaved,
            replyMarkup: new InlineKeyboardMarkup(keyboard),
            cancellationToken: cancellationToken);
    }
}