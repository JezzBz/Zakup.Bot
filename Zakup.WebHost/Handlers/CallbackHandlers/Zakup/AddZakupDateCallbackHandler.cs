using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Channel;
using Zakup.Common.DTO.Zakup;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.CallbackHandlers.Zakup;

[CallbackType(CallbackType.ChooseZakupDate)]
public class AddZakupDateCallbackHandler : ICallbackHandler<AddZakupDateCallbackData>
{
    private readonly UserService _userService;
    private readonly HandlersManager _handlersManager;
    private readonly ZakupService _zakupService;

    public AddZakupDateCallbackHandler(UserService userService, HandlersManager handlersManager, ZakupService zakupService)
    {
        _userService = userService;
        _handlersManager = handlersManager;
        _zakupService = zakupService;
    }

    public async Task Handle(ITelegramBotClient botClient, AddZakupDateCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var zakup = new TelegramZakup
        {
            ChannelId = data.ChannelId,
            CreatedUtc = DateTime.UtcNow,
            ZakupSource = ZakupSource.Bot,
            Price = 1
        };
        zakup = await _zakupService.Create(zakup, cancellationToken);
        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        state.State = UserStateType.ChooseZakupDate;
        state.CachedValue = CacheHelper.ToCache(new ZakupIdCache()
        {
            ZakupId = zakup.Id
        });
        await _userService.SetUserState(state, cancellationToken);
        var cancelData = _handlersManager.ToCallback(new DeleteZakupCallbackData
        {
            ZakupId = zakup.Id
        });
        
        var skipData = _handlersManager.ToCallback(new AddZakupPriceCallbackData()
        {
            ZakupId = zakup.Id
        });

        var keyboard = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Cancel,
                cancelData),
            InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Skip, skipData)
        };
        
        await botClient.SafeEdit(callbackQuery.From.Id, 
            callbackQuery.Message!.MessageId, 
            MessageTemplate.ChooseZakupDate, 
            replyMarkup: new InlineKeyboardMarkup(keyboard),
            cancellationToken: cancellationToken);
    }

    public AddZakupDateCallbackData Parse(List<string> parameters)
    {
        return new AddZakupDateCallbackData
        {
            ChannelId = long.Parse(parameters[0])
        };
    }
}