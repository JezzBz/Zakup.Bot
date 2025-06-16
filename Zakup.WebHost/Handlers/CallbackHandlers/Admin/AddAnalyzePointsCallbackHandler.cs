using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Common;
using Zakup.Common.DTO;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.CallbackHandlers.Admin;

[CallbackType(CallbackType.AdminAddAnalyzePoints)]
public class AddAnalyzePointsCallbackHandler : IEmptyCallbackHandler
{
    private readonly UserService _userService;

    public AddAnalyzePointsCallbackHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, EmptyCallbackData data, CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var state = await _userService.GetUserState(callbackQuery.From.Id, cancellationToken);
        state.State = UserStateType.RecieveForwardToBalance;
        await _userService.SetUserState(state, cancellationToken);
        await botClient.SendTextMessageAsync(callbackQuery.From.Id, MessageTemplate.ForwardBalabceMessage, cancellationToken: cancellationToken);
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
    }
}