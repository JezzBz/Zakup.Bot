using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Zakup.Services.Extensions;

public static class DataContextExtensions
{
     /// <summary>
    /// Сохраняет изменения в базе данных с обработкой конфликтов параллельного доступа
    /// </summary>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="maxRetries">Максимальное количество попыток</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Количество затронутых записей</returns>
    public static async Task<int> SaveChangesWithRetriesAsync(
        this DbContext context, 
        int maxRetries = 3, 
        CancellationToken cancellationToken = default)
    {
        int result = 0;
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                result = await context.SaveChangesAsync(cancellationToken);
                return result; // Если нет исключения, выходим из метода
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (i == maxRetries - 1) // Если это последняя попытка
                {
                    Console.WriteLine($"[SaveChangesWithRetries] Failed after {maxRetries} attempts: {ex.Message}");
                    throw; // Перебрасываем исключение после исчерпания попыток
                }
                
                // Обработка конфликтов
                foreach (var entry in ex.Entries)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    
                    if (databaseValues == null)
                    {
                        // Если запись была удалена из БД, нельзя продолжать для этой записи
                        Console.WriteLine($"[SaveChangesWithRetries] Запись больше не существует в базе данных");
                        continue;
                    }
                    
                    // Обновляем значения из базы данных
                    entry.OriginalValues.SetValues(databaseValues);
                }
                
                Console.WriteLine($"[SaveChangesWithRetries] Retry {i+1}/{maxRetries}");
                await Task.Delay(100 * (i + 1), cancellationToken); // Небольшая задержка перед повторной попыткой
            }
        }
        
        return result;
    }
     
    public static async Task ExecuteInTransactionAsync(
        this DbContext context, 
        Func<Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel);
        try
        {
            await action();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}