
using Amazon.S3;
using Bot.Core;
using Bot.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Minio;
using NLog.Web;
using Telegram.Bot.Types.Enums;
using Zakup.EntityFramework;
using Zakup.Services;
using Zakup.Services.Options;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Handlers;
using Zakup.WebHost.Handlers.MessageHandlers;
using Zakup.WebHost.Helpers;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();

builder.ConfigureLogging();
builder.ConfigureMvc();

builder.Logging.ClearProviders();
builder.Host.UseNLog();
builder.Logging.AddNLogWeb("NLog.config");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration["Storage:ConnectionStrings:DbContext"]));


builder.Services.AddTelegramBot(builder.Configuration["Telegram:BotToken"]!, config =>
{
    config.WithOptions(new()
    {
        Limit = 30,
        // enable all event listing
        AllowedUpdates = Enum.GetValues(typeof(UpdateType)).Cast<UpdateType>().ToArray()
    });
   
   
    config.AddHandler<StartMessageHandler>();
    config.AddHandler<MessageHandler>();
    config.AddHandler<CallbackHandler>();
    config.AddHandler<MediaGroupUploadHandler>();
    
    config.WithPreHandler<PreHandler>();
    config.UseLogging();
});

#region Options

builder.Services.AddOptions<BotConfigOptions>()
    .BindConfiguration(BotConfigOptions.Key);
builder.Services.AddOptions<FileStorageConfig>()
    .BindConfiguration(FileStorageConfig.Key);
    

// Или для MinioClient
builder.Services.AddSingleton<MinioClient>(provider => 
    new MinioClient()
        .WithEndpoint(builder.Configuration["MinIO:Endpoint"])
        .WithCredentials(
            builder.Configuration["MinIO:AccessKey"],
            builder.Configuration["MinIO:SecretKey"])
        .WithSSL(bool.Parse(builder.Configuration["MinIO:UseSSL"]))
        .Build() as MinioClient);
#endregion

#region Services

builder.Services.AddSingleton<HandlersManager>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<MessagesService>();
builder.Services.AddScoped<ZakupService>();
builder.Services.AddScoped<ChannelService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<DocumentsStorageService>();
builder.Services.AddScoped<AdPostsService>();

#endregion

builder.Services.AddHostedService<AbstractWorker<TelegramBotWorker>>();

var app = builder.Build();


app.Run();