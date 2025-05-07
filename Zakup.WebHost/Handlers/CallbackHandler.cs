using Bot.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Common.Models;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers;

/// <summary>
/// Маршрутизатор callback запросов
/// </summary>
public class CallbackHandler : IUpdatesHandler
{
    private readonly CallbackManager _callbackManager;

    public CallbackHandler(CallbackManager callbackManager)
    {
        _callbackManager = callbackManager;
    }

    public bool ShouldHandle(Update update) => update.IsCallback();

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var data = update.CallbackQuery?.Data;
        
        if (data == null)
        {
            throw new ArgumentNullException(nameof(update.CallbackQuery.Data));
        }
        
        var command = CallbackCommandsHelper.Parse(data);
        var handler = _callbackManager.GetInstance(command.Command);
        await handler.Handle(botClient, command.Paramers.ToList(), update.CallbackQuery!);
    }
}