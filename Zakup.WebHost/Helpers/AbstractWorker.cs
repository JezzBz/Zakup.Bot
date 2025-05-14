namespace Bot.Core;

public class AbstractWorker<T>(IServiceProvider provider, ILogger<T> logger) : BackgroundService
    where T : Worker
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = provider.CreateAsyncScope();
        var worker = ActivatorUtilities.CreateInstance<T>(scope.ServiceProvider);

        try
        {
            await worker.OnStart(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await worker.OnUpdate(stoppingToken);
                }
                catch (Exception e)
                {
                    logger.LogCritical(e, "Failed execute hosted worker");
                }
            }
            await worker.OnDestroy(stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Failed execute hosted worker");
        }
    }
}