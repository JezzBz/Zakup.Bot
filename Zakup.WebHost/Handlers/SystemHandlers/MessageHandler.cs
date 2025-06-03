using Bot.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Common.Enums;
using Zakup.Common.Exceptions;
using Zakup.Entities;
using Zakup.Services;
using Zakup.Services.Extensions;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Handlers.MessageHandlers.Channel;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers;

public class MessageHandler :  IUpdatesHandler
{
    private readonly UserService _userService;
    private readonly HandlersManager _handlersManager;

    public MessageHandler(UserService userService, HandlersManager handlersManager)
    {
        _userService = userService;
        _handlersManager = handlersManager;
    }

    public static bool ShouldHandle(Update update) => update.IsNotEmptyMessage();

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var userState = await _userService.GetUserState(update.Message!.Chat.Id, cancellationToken);

        if (userState!.State == UserStateType.None)
        {
            var stateLessHandler = _handlersManager.GetStatelessHandlerInstance();
            await stateLessHandler.Handle(botClient, update, cancellationToken);
            return;
        }
        
        var handler = _handlersManager.GetInstance(userState!.State);
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