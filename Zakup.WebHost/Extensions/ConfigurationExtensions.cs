using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using NLog.Web;
using Npgsql;
using Quartz;
using Zakup.Services;
using Zakup.WebHost.Jobs;

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

    public static WebApplicationBuilder ConfigureQuartz(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<DailyReportJob>();
        builder.Services.AddTransient<RequestsApproveJob>();
        builder.Services.AddQuartzHostedService(options =>
            options.WaitForJobsToComplete = false);
        builder.Services.AddQuartz(q =>
        {
            q.SchedulerId = "Request Approve bot";
            q.UseSimpleTypeLoader();
            q.UseInMemoryStore();
            q.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = 10;
            });
            
            q.ScheduleJob<RequestsApproveJob>(trigger => trigger
                .WithIdentity("Every 5 seconds trigger")
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(5).RepeatForever())
                .WithDescription("Approve requests every 5 seconds")
            );
            
            // Добавляем ежедневный отчет в 9:00 по Москве
            q.ScheduleJob<DailyReportJob>(trigger => trigger
                .WithIdentity("Daily report at 9:00 AM MSK")
                //.WithCronSchedule("0 0 9 * * ?", x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow")))
                .WithDescription("Send daily subscriber report at 9:00 AM Moscow time")
            );
        });
        
        return builder;
    }

    public static WebApplicationBuilder ConfigureSheetsServices(this WebApplicationBuilder builder)
    {
        GoogleCredential driveCredentials;
        GoogleCredential sheetsCredentials;
        using (var credentialsStream = new FileStream("sheetsCredentials.json", FileMode.Open, FileAccess.Read))
        {
            sheetsCredentials = GoogleCredential.FromStream(credentialsStream)
                .CreateScoped(SheetsService.ScopeConstants.Spreadsheets);
        }
        
        using (var credentialsStream = new FileStream("sheetsCredentials.json", FileMode.Open, FileAccess.Read))
        {
            driveCredentials = GoogleCredential.FromStream(credentialsStream)
                .CreateScoped(DriveService.ScopeConstants.Drive);
        }
        

        if (sheetsCredentials is null)
        {
            throw new NullReferenceException(nameof(sheetsCredentials));
        }
        
        if (driveCredentials is null)
        {
            throw new NullReferenceException(nameof(driveCredentials));
        }

        builder.Services.AddScoped<DriveService>(x => new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = driveCredentials,
            ApplicationName = "Zakup_Robot_Drive",
        }));
        
        builder.Services.AddScoped<SheetsService>(x => new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = sheetsCredentials,
            ApplicationName = "Zakup_Robot",
        }));

        builder.Services.AddScoped<InternalSheetsService>();
        
        return builder;
    }
}