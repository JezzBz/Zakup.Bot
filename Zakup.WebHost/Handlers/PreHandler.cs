using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.EntityFramework;

namespace Zakup.WebHost.Handlers;

using Bot.Core;

public class PreHandler : IPreHandler
{
    private readonly IServiceProvider _provider;

    public PreHandler(IServiceProvider provider)
    {
        _provider = provider;
    }

    public async Task<bool> CanContinue(Update updates, ITelegramBotClient botClient)
    {
        await using var scope = _provider.CreateAsyncScope();
        var _dataContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        long channelId = default;
        
        if (updates.Type == UpdateType.ChatJoinRequest)
        {
            channelId = updates.ChatJoinRequest!.Chat.Id;
        }
        
        if (updates.Type == UpdateType.ChannelPost)
        {
            channelId = updates.ChannelPost!.Chat.Id;
        }
        
        if (updates.Type == UpdateType.EditedChannelPost)
        {
            channelId = updates.EditedChannelPost!.Chat.Id;
        }

        return channelId == default; //|| await _dataContext.TelegramChannels.AnyAsync(x => x.Id == channelId);
    }
}