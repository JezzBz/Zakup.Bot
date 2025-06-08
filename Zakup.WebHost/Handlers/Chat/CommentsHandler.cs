using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.EntityFramework;

namespace app.Services.Handlers.Chat;

using Bot.Core;

public class CommentsHandler : IUpdatesHandler
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CommentsHandler> _logger;
    
    public CommentsHandler(ILogger<CommentsHandler> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public static bool ShouldHandle(Update update)
    {
        return update.Type == UpdateType.Message && update.Message!.Chat.Type is ChatType.Group or ChatType.Supergroup;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message!;
        

        var channel = await _context.Channels.FirstOrDefaultAsync(q => q.ChannelChatId == message.Chat.Id, cancellationToken: cancellationToken);

        if (channel is null)
        {
            _logger.LogWarning($"Канал привязанный к чату не найден. ChatId: {message.Chat.Id} Message:{message.Text}");
            return;
        }

        var member = await _context.ChannelMembers
            .Where(q => q.ChannelId == channel.Id)
            .FirstOrDefaultAsync(q => q.UserId == message.From!.Id, cancellationToken: cancellationToken);

        if (member is null)
        {
            _logger.LogWarning($"Пользователь не найден в участниках канала. ChatId: {message.Chat.Id} Message:{message.Text} UserName:{message.From?.Username} ChannelId:{channel.Id}");
            return;
        }
        
        member.IsCommenter = true;
        _context.Update(member);
        await _context.SaveChangesAsync(cancellationToken);
    }
    
}
