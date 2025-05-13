using Zakup.Abstractions.Data;
using Zakup.Abstractions.Handlers;
using Zakup.Common.Enums;

namespace Zakup.WebHost.Helpers;

/// <summary>
/// Гарант соответствия типа callbackType и его обработчика
/// </summary>
public class HandlersManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<CallbackType, Type> _callbackHandlers;
    private readonly Dictionary<UserStateType, Type> _stateHandlers;
    
    public HandlersManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _callbackHandlers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttributes(typeof(CallbackTypeAttribute), false).Length > 0 && 
                        typeof(ICallbackHandler).IsAssignableFrom(t))
            .ToDictionary(
                t => ((CallbackTypeAttribute)t.GetCustomAttributes(typeof(CallbackTypeAttribute), false)[0]).Type,
                t => t);
        
        _stateHandlers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttributes(typeof(StateTypeAttribute), false).Length > 0 && 
                        typeof(IStateHandler).IsAssignableFrom(t))
            .ToDictionary(
                t => ((StateTypeAttribute)t.GetCustomAttributes(typeof(StateTypeAttribute), false)[0]).State,
                t => t);
    }
    
    /// <summary>
    /// Получить хендлер по типу callback
    /// </summary>
    /// <param name="callbackType">Тип callback'а</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public ICallbackHandler GetInstance(CallbackType callbackType)
    {
        if (_callbackHandlers.TryGetValue(callbackType, out Type handlerType))
        {
            return (ICallbackHandler)ActivatorUtilities.CreateInstance(_serviceProvider, handlerType)!;
        }
        throw new ArgumentException($"Unknown handler type: {callbackType}");
    }
    
    /// <summary>
    /// Получить хендлер по типу остояния
    /// </summary>
    /// <param name="stateType">Тип callback'а</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public IStateHandler GetInstance(UserStateType stateType)
    {
        if (_stateHandlers.TryGetValue(stateType, out Type handlerType))
        {
            return (IStateHandler)ActivatorUtilities.CreateInstance(_serviceProvider, handlerType)!;
        }
        throw new ArgumentException($"Unknown handler type: {stateType}");
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
        var handlerEntry = _callbackHandlers.FirstOrDefault(x => x.Value == typeof(ICallbackHandler<TData>));
        if (handlerEntry.Value == null)
        {
            throw new ArgumentException($"Unknown handler type: {typeof(TData).Name}");
        }
        return $"{handlerEntry.Key}|" + data.ToCallback();
    }
}