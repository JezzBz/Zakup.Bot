using Bot.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Common.Enums;
using Zakup.Common.Exceptions;
using Zakup.Services;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Handlers.MessageHandlers.Channel;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers;

public class MessageHandler :  IUpdatesHandler
{
    private readonly UserStateService _userStateService;
    private readonly HandlersManager _handlersManager;

    public MessageHandler(UserStateService userStateService, HandlersManager handlersManager)
    {
        _userStateService = userStateService;
        _handlersManager = handlersManager;
    }

    public bool ShouldHandle(Update update) => update.IsNotEmptyMessage();

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var userState = await _userStateService.GetUserState(update.Message.Chat.Id);

        var handler = _handlersManager.GetInstance(userState.State);
        try
        {
            await handler.Handle(botClient, update, cancellationToken);
        }
        catch (IncorrectDataException e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
    
}