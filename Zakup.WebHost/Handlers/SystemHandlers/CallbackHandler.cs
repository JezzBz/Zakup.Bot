using Bot.Core;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Abstractions.DataContext;
using Zakup.Common.Models;
using Zakup.Services.Extensions;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Helpers;

namespace Zakup.WebHost.Handlers;

/// <summary>
/// Маршрутизатор callback запросов
/// </summary>
public class CallbackHandler : IUpdatesHandler
{
    private readonly HandlersManager _handlersManager;
    private readonly IBigCallbackDataService _bigCallbackDataService;
    public CallbackHandler(HandlersManager handlersManager, IBigCallbackDataService bigCallbackDataService)
    {
        _handlersManager = handlersManager;
        _bigCallbackDataService = bigCallbackDataService;
    }

    //Реагируем на любые callback сообщения
    public static bool ShouldHandle(Update update) => update.IsCallback();

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
        
        //Для даных больше 64 символов
        if (data.StartsWith("BCD"))
        {
            var dataId = long.Parse(data.Split("|")[1]);
            data = await _bigCallbackDataService.GetBigCallbackData(dataId);
        }
        
        var command = CommandsHelper.ParseCallback(data);
        var handler = _handlersManager.GetInstance(command.Command);
        await handler.Handle(botClient, command.Params.ToList(), update.CallbackQuery!, cancellationToken);
    }
}