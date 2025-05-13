
using Bot.Core.Extensions;
using Telegram.Bot.Types.Enums;
using Zakup.WebHost.Extensions;
using Zakup.WebHost.Handlers;
using Zakup.WebHost.Helpers;

var builder = WebApplication.CreateBuilder(args);


builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();

builder.ConfigureLogging();
builder.ConfigureMvc();

builder.Services.AddTelegramBot(builder.Configuration["Telegram:BotToken"]!, config =>
{
    config.WithOptions(new()
    {
        Limit = 30,
        // enable all event listing
        AllowedUpdates = Enum.GetValues(typeof(UpdateType)).Cast<UpdateType>().ToArray()
    });

    config.AddHandler<CallbackHandler>();
    
    config.WithPreHandler<PreHandler>();
    config.UseLogging();
});

builder.Services.AddSingleton<HandlersManager>();

var app = builder.Build();


app.Run();