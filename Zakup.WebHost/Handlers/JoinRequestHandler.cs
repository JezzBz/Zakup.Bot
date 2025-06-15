using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Entities;
using Zakup.EntityFramework;

namespace app.Services.Handlers;

using Bot.Core;

public class JoinRequestHandler : IUpdatesHandler
{
    private readonly ApplicationDbContext _dbContext;

    public JoinRequestHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public static bool ShouldHandle(Update update)
    {
        return update.Type == UpdateType.ChatJoinRequest;
    }
    

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
    
        var request = new ChannelJoinRequest
        {
            UserId = update.ChatJoinRequest!.From.Id,
            ChannelId = update.ChatJoinRequest!.Chat.Id,
            InviteLink = update.ChatJoinRequest!.InviteLink?.InviteLink,
            RequestedUtc = DateTime.UtcNow
        };

        await _dbContext.AddAsync(request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

   
}
