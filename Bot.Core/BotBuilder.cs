using System.Reflection;
using System.Runtime.InteropServices.JavaScript;

namespace Bot.Core;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

public class BotBuilder
{
    private List<Type> _handlerTypes;
    private IUpdatesHandler _defaultHandler;
    private readonly IServiceCollection _servicesCollection;
    private IServiceProvider _serviceProvider;
    private readonly ITelegramBotClient _client;
    private Type? _preHandlerType = null;
    private IPreHandler? _preHandler;
    
    #region Bot configuration
    private ReceiverOptions? _receiverOptions;
    #endregion
    
    #region Logger configuration

    private ILoggerFactory? _loggerFactory;
    private bool _useLogging;

    #endregion

    public BotBuilder(IServiceCollection servicesCollection, ITelegramBotClient client)
    {
        _handlerTypes = new List<Type>();
        _servicesCollection = servicesCollection;
        _client = client;
    }
    
    public BotBuilder AddHandler<T>() where T : IUpdatesHandler
    {
        _handlerTypes.Add(typeof(T));
        return this;
    }
    
    public BotBuilder UseLogging()
    {
        _useLogging = true;
        return this;
    }
    
    public BotBuilder WithOptions(ReceiverOptions receiverOptions)
    {
        _receiverOptions = receiverOptions;
        return this;
    }

    public BotBuilder WithPreHandler<T>() where T : IPreHandler
    {
        _preHandlerType = typeof(T);
        return this;
    }
    
    private async Task HandleUpdates(ITelegramBotClient botClient, Update updates, CancellationToken cancellationToken)
    {
        if (_preHandler != null)
        {
            if (!await _preHandler.CanContinue(updates,botClient))
            {
                return;
            }
        }
        
        
        var handlerType = typeof(DefaultHandler);
        try
        {
            handlerType = _handlerTypes.FirstOrDefault(q => CallShouldHandle(q, updates)) ?? handlerType;
            var handler = (IUpdatesHandler)ActivatorUtilities.CreateInstance(_serviceProvider, handlerType);
            await handler.Handle(botClient, updates, cancellationToken);
        }
        catch (Exception e)
        {
            if (_loggerFactory is not null)
            {
                var logger = _loggerFactory.CreateLogger(handlerType);

                // Попытка сериализовать объект Update в JSON
                string updateJson;
                try
                {
                    updateJson = JsonSerializer.Serialize(updates, new JsonSerializerOptions { WriteIndented = true });
                }
                catch (Exception serializationEx)
                {
                    // Обработка ошибок сериализации
                    updateJson = $"Ошибка сериализации Update: {serializationEx.Message}";
                }

                if (e is ApiRequestException apiEx && apiEx.ErrorCode == 502)
                {
                    logger.LogError($"Сервер Telegram недоступен. Время: {DateTime.UtcNow.ToLocalTime()}. Update: {updateJson}");
                }
                else
                {
                    var st = new StackTrace(e, true);
                    var frame = st.GetFrame(0);
                    var line = frame?.GetFileLineNumber() ?? 0;
                    logger.LogError(e, $"{handlerType.Name} |1 line {line}: {e.Message} {e.InnerException?.Message}. Update: {updateJson}");
                }
            }
            else
            {
                // Резервное логирование, если _loggerFactory равен null
                Console.WriteLine($"Ошибка в обработчике {handlerType.Name}: {e.Message}");
                try
                {
                    string updateJson = JsonSerializer.Serialize(updates, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine($"Update: {updateJson}");
                }
                catch (Exception serializationEx)
                {
                    Console.WriteLine($"Ошибка сериализации Update: {serializationEx.Message}");
                }
            }
        }
        
    }
    
    internal BotContainer Build()
    {
        var provider = _servicesCollection.BuildServiceProvider();
        
        if (_useLogging)
        {
            _loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        }
        
        BuildHandlers(provider);
        _serviceProvider = provider;
        return new BotContainer(HandleUpdates, HandlePollingErrorAsync, _client, _receiverOptions);
    }
    
    private void BuildHandlers(IServiceProvider provider)
    {
        _defaultHandler = (IUpdatesHandler)ActivatorUtilities.CreateInstance(provider, typeof(DefaultHandler))!;

        if (_preHandlerType != null)
        {
            _preHandler = (IPreHandler)ActivatorUtilities.CreateInstance(provider, _preHandlerType);
        }
    }
    
    Task HandlePollingErrorAsync(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };
       
        if (_loggerFactory != null)
        {
            var logger = _loggerFactory.CreateLogger(typeof(TelegramBotClient));
            logger.LogCritical(exception, ErrorMessage);
        }
        else
        {
            throw exception;
        }
        
        return Task.CompletedTask;
    }

    bool CallShouldHandle(Type targetType, Update update)
    {
        // Получаем метод по имени (укажите правильное имя метода и параметры)
        MethodInfo method = targetType.GetMethod(nameof(IUpdatesHandler.ShouldHandle), 
            BindingFlags.Public | BindingFlags.Static);
        
        if (method != null)
        {
            return (bool)method.Invoke(null, new object[] { update });
        }
        else
        {
            
            Console.WriteLine($"Метод не найден в типе {targetType.Name}");
            throw new Exception("Неизвестный тип хендлера");
        }
    }
}
