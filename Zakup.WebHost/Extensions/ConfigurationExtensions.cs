using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using NLog.Web;
using Npgsql;

namespace Zakup.WebHost.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Конфигурация логов
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Host.UseNLog();
        builder.Logging.AddNLogWeb("nlog.config");
        
        return builder;
    }

    /// <summary>
    /// Конфигурация контроллеров
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ConfigureMvc(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        builder.Services.AddMvc();
        builder.Services.AddEndpointsApiExplorer();
        return builder;
    }

     /// <summary>
     /// Конфигурация контекста бд
     /// </summary>
     /// <param name="builder"></param>
     /// <param name="connectionString">строка подключения</param>
     /// <typeparam name="TDbContext">тип контекста</typeparam>
     /// <returns></returns>
    public static WebApplicationBuilder ConfigureDataContext<TDbContext>(this WebApplicationBuilder builder, string connectionString)
        where TDbContext : DbContext
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        
        var dataSource = dataSourceBuilder.Build();
		
        builder.Services.AddDbContext<TDbContext>(options =>
        {
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            options.UseNpgsql(dataSource);
            options.EnableSensitiveDataLogging();
            options.UseLoggerFactory(new NLogLoggerFactory());
        });
        
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
		
        optionsBuilder.UseNpgsql(connectionString);
        
        using var context = (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options)!;

        if (context == null)
        {
            throw new ApplicationException("Cannot create instance of the database context");
        }
        
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
            ((NpgsqlConnection)context.Database.GetDbConnection()).Open();
            ((NpgsqlConnection)context.Database.GetDbConnection()).ReloadTypes();
        }
        
        return builder;
    }
}