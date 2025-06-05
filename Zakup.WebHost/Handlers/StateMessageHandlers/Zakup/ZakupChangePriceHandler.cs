using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.StateMessageHandlers.Zakup;

[StateType(UserStateType.ChangeZakupPrice)]
public class ZakupChangePriceHandler : IStateHandler
{
    private readonly ZakupService _zakupService;
    private readonly UserService _userService;

    public ZakupChangePriceHandler(UserService userService, ZakupService zakupService)
    {
        _userService = userService;
        _zakupService = zakupService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (!decimal.TryParse(update.Message!.Text, out var amount))
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
        
        await botClient.SendTextMessageAsync(update.Message.From!.Id, 
            MessageTemplate.PriceSaved,
            cancellationToken: cancellationToken);
    }
}