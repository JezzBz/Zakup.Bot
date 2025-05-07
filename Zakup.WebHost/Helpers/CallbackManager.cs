using Zakup.Abstractions.Data;
using Zakup.Abstractions.Handlers;
using Zakup.Common.Enums;

namespace Zakup.WebHost.Helpers;

/// <summary>
/// Гарант соответствия типа callbackType и его обработчика
/// </summary>
public class CallbackManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<CallbackType, Type> _handlers;
    
    public CallbackManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _handlers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttributes(typeof(CallbackTypeAttribute), false).Length > 0)
            .ToDictionary(
                t => ((CallbackTypeAttribute)t.GetCustomAttributes(typeof(CallbackTypeAttribute), false)[0]).Type,
                t => t);
    }
    
    /// <summary>
    /// Получить хендлер по типу
    /// </summary>
    /// <param name="callbackType">Тип callback'а</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public ICallbackHandler GetInstance(CallbackType callbackType)
    {
        if (_handlers.TryGetValue(callbackType, out Type handlerType))
        {
            return (ICallbackHandler)ActivatorUtilities.CreateInstance(_serviceProvider, handlerType)!;
        }
        throw new ArgumentException($"Unknown handler type: {callbackType}");
    }

    /// <summary>
    /// Гарантирует правильную генерацию направленного callback сообщения
    /// </summary>
    /// <param name="data">Параметры callback</param>
    /// <typeparam name="TData">Тип параметров</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public string ToCallback<TData>(TData data) where TData : ICallbackData
    {
        var handlerEntry = _handlers.FirstOrDefault(x => x.Value == typeof(ICallbackHandler<TData>));
        if (handlerEntry.Value == null)
        {
            throw new ArgumentException($"Unknown handler type: {typeof(TData).Name}");
        }
        return $"{handlerEntry.Key}|" + data.ToCallback();
    }
}