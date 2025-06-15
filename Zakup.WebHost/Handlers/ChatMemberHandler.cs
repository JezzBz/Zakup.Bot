using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Entities;
using Zakup.EntityFramework;

namespace app.Services.Handlers;

using Bot.Core;

public class ChatMemberHandler : IUpdatesHandler
{
    private readonly ILogger<ChatMemberHandler> _logger;
    private readonly ApplicationDbContext _dataContext;
    
    public ChatMemberHandler(ApplicationDbContext dataContext, ILogger<ChatMemberHandler> logger)
    {
        _dataContext = dataContext;
        _logger = logger;
    }

    public static bool ShouldHandle(Update update)
    {
        return update.Type == UpdateType.ChatMember;
    }

    public async Task Handle(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        
        var channelExists = await _dataContext.Channels.AnyAsync(q => q.Id == update.ChatMember!.Chat.Id,
            cancellationToken: cancellationToken);

        if (update.ChatMember!.Chat.Type != ChatType.Channel)
        {
            return;
        }
        
        if (!channelExists)
        {
            _logger.LogWarning($"Канал не прошел процедуру добавления в пул бота. ChannelId: {update.ChatMember.Chat.Id} Channel:{update.ChatMember.Chat.Username}");   
            return;
        }
        
         var member =
            await _dataContext.ChannelMembers
                .Where(cm => cm.ChannelId == update.ChatMember!.Chat.Id)
                .FirstOrDefaultAsync(cm => cm.UserId == update.ChatMember!.NewChatMember.User.Id, cancellationToken: cancellationToken);

        var zakup = await _dataContext.TelegramZakups
            .Where(z => z.ChannelId == update.ChatMember!.Chat.Id)
            .FirstOrDefaultAsync(z => update.ChatMember!.InviteLink != null && z.InviteLink == update.ChatMember!.InviteLink.InviteLink, cancellationToken: cancellationToken);
        
        var refer = zakup?.Id.ToString() ?? (update.ChatMember!.InviteLink != null ? "404" : "unknown");
        switch (update.ChatMember!.NewChatMember.Status)
        {
            case ChatMemberStatus.Left:
            case ChatMemberStatus.Kicked:
            {
                if (member is null)
                {
                    member = new ChannelMember
                    {
                        UserId = update.ChatMember!.NewChatMember.User.Id,
                        IsPremium = update.ChatMember!.NewChatMember.User.IsPremium,
                        UserName = update.ChatMember!.NewChatMember.User.Username,
                        ChannelId = update.ChatMember!.Chat.Id,
                        JoinCount = 1,
                        LeftUtc = update.ChatMember.Date.ToUniversalTime(),
                        InviteLink = update.ChatMember!.InviteLink?.InviteLink,
                        InviteLinkName = update.ChatMember!.InviteLink?.Name,
                        Zakup = zakup,
                        Refer = "origin"
                    };

                    try
                    {
                        await _dataContext.AddAsync(member, cancellationToken);
                        await _dataContext.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateException ex)
                    {
                        Console.WriteLine($"Ошибка при добавлении данных пользователя в канале. Канал ID: {update.ChatMember!.Chat.Id}, Пользователь ID: {update.ChatMember!.NewChatMember.User.Id}. Ошибка: {ex.InnerException.Message}");
                    }
                    return;
                }

                member.LeftUtc = DateTime.UtcNow;
                member.Status = false;

                _dataContext.Update(member);
                await _dataContext.SaveChangesAsync(cancellationToken);

                return;
            }
            case ChatMemberStatus.Member when
                update.ChatMember!.OldChatMember.Status == ChatMemberStatus.Left:
            {
                if (member is null)
                {
                    member = new ChannelMember
                    {
                        UserId = update.ChatMember!.NewChatMember.User.Id,
                        ChannelId = update.ChatMember!.Chat.Id,
                        IsPremium = update.ChatMember!.NewChatMember.User.IsPremium,
                        UserName = update.ChatMember!.NewChatMember.User.Username,
                        Status = true,
                        InviteLink = update.ChatMember!.InviteLink?.InviteLink,
                        InviteLinkName = update.ChatMember!.InviteLink?.Name,
                        JoinCount = 1,
                        JoinedUtc = DateTime.UtcNow,
                        Zakup = zakup,
                        Refer = refer
                    };
                    await _dataContext.AddAsync(member, cancellationToken);
                    await _dataContext.SaveChangesAsync(cancellationToken);
                    return;
                }

                member.InviteLink = update.ChatMember!.InviteLink?.InviteLink ?? member.InviteLink;
                member.InviteLinkName = update.ChatMember!.InviteLink?.Name ?? member.InviteLinkName;
                member.Status = true;
                member.JoinCount++;
                member.JoinedUtc = DateTime.UtcNow;
                _dataContext.Update(member);
                await _dataContext.SaveChangesAsync(cancellationToken);
                return;
            }
            case ChatMemberStatus.Member:
            {
                if (member is null)
                {
                    member = new ChannelMember
                    {
                        UserId = update.ChatMember!.NewChatMember.User.Id,
                        ChannelId = update.ChatMember!.Chat.Id,
                        IsPremium = update.ChatMember!.NewChatMember.User.IsPremium,
                        UserName = update.ChatMember!.NewChatMember.User.Username,
                        Status = true,
                        InviteLink = update.ChatMember!.InviteLink?.InviteLink,
                        InviteLinkName = update.ChatMember!.InviteLink?.Name,
                        JoinCount = 1,
                        JoinedUtc = DateTime.UtcNow,
                        Zakup = zakup,
                        Refer = refer
                    };
                    await _dataContext.AddAsync(member, cancellationToken);
                    await _dataContext.SaveChangesAsync(cancellationToken);
                  
                }
                return;
            }
        }
    }
}
