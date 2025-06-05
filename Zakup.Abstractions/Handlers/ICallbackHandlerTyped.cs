using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Data;

namespace Zakup.Abstractions.Handlers;

public interface ICallbackHandler<in T> : ICallbackHandler
    where T : ICallbackData, new()
{
    Task Handle(ITelegramBotClient botClient,T data, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    
    // Явная реализация для необобщенного интерфейса
    Task ICallbackHandler.Handle(ITelegramBotClient botClient, List<string> parameters, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var data = new T(); 
        data.Parse(parameters);
        
        return Handle(botClient, data, callbackQuery, cancellationToken);
    }
}