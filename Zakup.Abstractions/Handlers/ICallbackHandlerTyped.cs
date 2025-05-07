using Telegram.Bot;
using Telegram.Bot.Types;
using Zakup.Abstractions.Data;

namespace Zakup.Abstractions.Handlers;

public interface ICallbackHandler<T> : ICallbackHandler
    where T : ICallbackData
{
    Task Handle(ITelegramBotClient botClient,T data, CallbackQuery callbackQuery);
    
    T Parse(List<string> parameters);
    
    // Явная реализация для необобщенного интерфейса
    Task ICallbackHandler.Handle(ITelegramBotClient botClient, List<string> parameters, CallbackQuery callbackQuery)
    {
        var data = Parse(parameters);
     
        if (data is T typedData)
        {
            return Handle(botClient, typedData, callbackQuery);
        }
        
        throw new ArgumentException($"Invalid data type. Expected {typeof(T)}");
    }
}