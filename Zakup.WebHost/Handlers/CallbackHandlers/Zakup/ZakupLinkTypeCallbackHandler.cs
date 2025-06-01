using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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

[CallbackType(CallbackType.ZakupLinkType)]
public class ZakupLinkTypeCallbackHandler : ICallbackHandler<ZakupLinkTypeCallbackData>
{
    private readonly ZakupService _zakupService;
    private readonly HandlersManager _handlersManager;
    private readonly UserService _userService;

    public ZakupLinkTypeCallbackHandler(ZakupService zakupService, HandlersManager handlersManager, UserService userService)
    {
        _zakupService = zakupService;
        _handlersManager = handlersManager;
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, ZakupLinkTypeCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var zakup = await _zakupService.Get(data.ZakupId, cancellationToken);
        zakup.NeedApprove = data.Private;
        await _zakupService.Update(zakup, cancellationToken);
        
        
        var cancelData = _handlersManager.ToCallback(new DeleteZakupCallbackData
        {
            ZakupId = data.ZakupId
        });
        var completeKeyboard = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(ButtonsTextTemplate.Menu, cancelData)
            }
        });
        
        
        var message = await botClient.SafeEdit(
            callbackQuery.From.Id,
            callbackQuery.Message!.MessageId,
            MessageTemplate.ZakupChannelAlias,
            ParseMode.MarkdownV2,
            replyMarkup: completeKeyboard, 
            cancellationToken: cancellationToken);
        
        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        state.State = UserStateType.ZakupChannelAlias;
        state.CachedValue = CacheHelper.ToCache(new ZakupIdCache
        {
            ZakupId = zakup.Id
        });
        state.PreviousMessageId = message.MessageId;
        await _userService.SetUserState(state, cancellationToken);
    }

    public ZakupLinkTypeCallbackData Parse(List<string> parameters)
    {
        return new ZakupLinkTypeCallbackData
        {
            ZakupId = Guid.Parse(parameters[0]),
            Private = bool.Parse(parameters[1]),
        };
    }
}