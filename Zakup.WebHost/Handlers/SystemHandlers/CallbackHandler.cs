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
    private readonly HandlersManager _handlersManager;

    public CallbackHandler(HandlersManager handlersManager)
    {
        _handlersManager = handlersManager;
    }

    //Реагируем на любые callback сообщения
    public bool ShouldHandle(Update update) => update.IsCallback();

    //Callback отвязан от состояния пользователя поэтому
    //*все данные, критически необходимые для обработчика нужно передавать в параметры
    //*UI бота становится независимым от состояния, даже если от пользователя ожидается ввод данных, callback отработает корректно
    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var data = update.CallbackQuery?.Data;
        
        if (data == null)
        {
            throw new ArgumentNullException(nameof(update.CallbackQuery.Data));
        }
        
        var command = CallbackCommandsHelper.Parse(data);
        var handler = _handlersManager.GetInstance(command.Command);
        await handler.Handle(botClient, command.Params.ToList(), update.CallbackQuery!, cancellationToken);
    }
}