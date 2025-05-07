using Telegram.Bot;
using Telegram.Bot.Types;

namespace Zakup.Abstractions.Handlers;

public interface ICallbackHandler
{
    Task Handle(ITelegramBotClient botClient, List<string> parameters, CallbackQuery callbackQuery);
   //public Task Handle(ITelegramBotClient botClient, List<string> parameters);
    
   // public string ToCallbackData(List<string> parameters);
}