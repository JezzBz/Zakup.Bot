using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Admin;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.StateMessageHandlers.Admin;

[StateType(UserStateType.RecieveForwardToBalance)]
public class AdminAddAnalyzePointsHandler : IStateHandler
{
    private readonly UserService _userService;

    public AdminAddAnalyzePointsHandler(UserService userService)
    {
        _userService = userService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var userToAddBalance = update.Message.ForwardFrom;
        if (userToAddBalance == null | userToAddBalance.IsBot)
        {
            await botClient.SendTextMessageAsync(update.Message.From.Id, MessageTemplate.AdminBalanceBadForward,
                cancellationToken: cancellationToken);
            return;
        }

        var state = await _userService.GetUserState(update.Message.From!.Id, cancellationToken);
        state!.State = UserStateType.RecieveBalanceValue;
        state.CachedValue = CacheHelper.ToCache(new AdminReceiveBalanceForUser
        {
            UserId = userToAddBalance.Id
        });
        await _userService.SetUserState(state, cancellationToken);
        await botClient.SendTextMessageAsync(state.UserId, MessageTemplate.ReceivePointForBalance, cancellationToken: cancellationToken);
    }
}