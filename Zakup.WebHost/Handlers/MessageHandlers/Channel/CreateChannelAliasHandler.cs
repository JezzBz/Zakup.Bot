using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Handlers;
using Zakup.Common.Enums;
using Zakup.Services;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers.MessageHandlers.Channel;

[StateType(UserStateType.CreateChannelAlias)]
public class CreateChannelAliasHandler : IStateHandler
{
    private readonly UserStateService _userStateService;

    public CreateChannelAliasHandler(UserStateService userStateService)
    {
        _userStateService = userStateService;
    }
    
    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        //var state = await _userStateService.GetUserState(update.Message!.Chat.Id);
    }
}