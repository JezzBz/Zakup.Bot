namespace Bot.Core.Extensions;

using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

public static class TelegramBotConfigurationExtensions
{
	/// <summary>
	/// Add hosted telegram bot
	/// </summary>
	/// <remarks>Default value for rate-limiter is 30 messages per minute. Call UseRateLimiter to configure different parameters</remarks>
	/// <param name="token">Telegram bot token</param>
	/// <param name="configureBuilder">Configuration action</param>
	/// <returns></returns>
	public static IServiceCollection AddTelegramBot(this IServiceCollection services, string token, Action<BotBuilder> configureBuilder)
    {
        
        var options = new TokenBucketRateLimiterOptions
        { 
            TokenLimit = 20, 
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 5, 
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(1000),
            TokensPerPeriod = 20, 
            AutoReplenishment = true
        };
        
        var httpclient = new HttpClient(
            handler: new ClientSideRateLimitedHandler(
                limiter: new TokenBucketRateLimiter(options)));
       
        var client = new TelegramBotClient(token, httpclient);
        
        services.AddSingleton<ITelegramBotClient>(client);
        
		var builder = new BotBuilder(services,client);
		configureBuilder.Invoke(builder);
		
		services.AddSingleton(builder);
		return services;
	}
}
