using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.DTO.Admin;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Constants;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.StateMessageHandlers.Admin;

[StateType(UserStateType.RecieveBalanceValue)]
public class ReceiveBalanceValueHandler : IStateHandler
{
    private readonly AnalyzeService _analyzeService;
    private readonly UserService _userService;
    private readonly MessagesService _messagesService;

    public ReceiveBalanceValueHandler(AnalyzeService analyzeService, UserService userService, MessagesService messagesService)
    {
        _analyzeService = analyzeService;
        _userService = userService;
        _messagesService = messagesService;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (!long.TryParse(update.Message?.Text, out var points))
        {
            await botClient.SendTextMessageAsync(update.Message.From.Id, MessageTemplate.AdminBadBalanceValue,
                cancellationToken: cancellationToken);
            return;
        }
        
        var admin = await _userService.GetUser(update.Message.From!.Id, cancellationToken);
        var userId = CacheHelper.ToData<AdminReceiveBalanceForUser>(admin!.UserState.CachedValue).UserId;

        var newBalance = await _analyzeService.UpdateBalance(userId, points, cancellationToken);

        await botClient.SendTextMessageAsync(admin.Id, MessageTemplate.BalanceUpdated(newBalance), cancellationToken: cancellationToken);
        await botClient.SendTextMessageAsync(userId, MessageTemplate.PointsAdded(points, newBalance), cancellationToken: cancellationToken);
    }
}