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

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.AddZakupPrice)]
public class AddZakupPriceCallbackHandler : ICallbackHandler<AddZakupPriceCallbackData>
{
    private readonly UserService _userService;
    private readonly HandlersManager _handlersManager;

    public AddZakupPriceCallbackHandler(UserService userService, HandlersManager handlersManager)
    {
        _userService = userService;
        _handlersManager = handlersManager;
    }

    public async Task Handle(ITelegramBotClient botClient, AddZakupPriceCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    { 
        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        state.Clear();
        state.State = UserStateType.AddZakupPrice;
        state.CachedValue = CacheHelper.ToCache(new ZakupIdCache
        {
            ZakupId = data.ZakupId
        });
        await _userService.SetUserState(state, cancellationToken);
        var cancelData = _handlersManager.ToCallback(new DeleteZakupCallbackData
        {
            ZakupId = data.ZakupId
        });
        
        var freeData = _handlersManager.ToCallback(new ZakupCheckChannelPrivateCallbackData()
        {
            ZakupId = data.ZakupId
        });
        
        var keyBoard = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Cancel, cancelData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Free, freeData),

        };
        
        await botClient.SafeEdit(callbackQuery.From.Id, callbackQuery.Message!.MessageId,MessageTemplate.ZakupPrice, replyMarkup: new InlineKeyboardMarkup(keyBoard), cancellationToken: cancellationToken);
    }

    public AddZakupPriceCallbackData Parse(List<string> parameters)
    {
        return new AddZakupPriceCallbackData()
        {
            ZakupId = Guid.Parse(parameters[0]),
        };
    }
}