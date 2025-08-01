using Zakup.Abstractions.Data;
using Zakup.Abstractions.DataContext;
using Zakup.Abstractions.Handlers;
using Zakup.Common.Enums;
using Zakup.Entities;
using Zakup.EntityFramework;
using Zakup.WebHost.Handlers.MessageHandlers;

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
    /// <param name="stateType">Тип стейта</param>
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
    /// Получить хендлер без состояния
    /// </summary>
    /// <param name="stateType">Тип callback'а</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public StatelessHandler GetStatelessHandlerInstance()
    {
        return (StatelessHandler)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(StatelessHandler))!;
        
    }

    /// <summary>
    /// Гарантирует правильную генерацию направленного callback сообщения
    /// </summary>
    /// <param name="data">Параметры callback</param>
    /// <typeparam name="TData">Тип параметров</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<string> ToCallback<TData>(TData data) where TData : ICallbackData, new()
    {
        var handlerEntry = _callbackHandlers.FirstOrDefault(x => typeof(ICallbackHandler<TData>).IsAssignableFrom(x.Value));
        if (handlerEntry.Value == null)
        {
            throw new ArgumentException($"Unknown handler type: {typeof(TData).Name}");
        }

        var callbackData = (int)handlerEntry.Key + "|" + data.ToCallback();
        if (callbackData.Length <= 64) return callbackData;
        
        var bcdService = _serviceProvider.GetRequiredService<IBigCallbackDataService>();
        
        var dataId = await bcdService.AddData(callbackData);
        
        return $"BCD|{dataId}";
    }
}