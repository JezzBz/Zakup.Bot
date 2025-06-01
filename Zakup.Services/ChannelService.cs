using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Zakup.Common.DTO.Channel;
using Zakup.Entities;
using Zakup.EntityFramework;

namespace Zakup.Services;

public class ChannelService
{
    private readonly ApplicationDbContext _context;

    public ChannelService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SubscribeStatistic> GetSubscribeStatistic(long administratorId, DateTimeOffset startUtc,
        DateTimeOffset endUtc, CancellationToken cancellationToken = default)
    {
        return new SubscribeStatistic
        {
            SubscribeCount = await _context.ChannelMembers
                .CountAsync(
                    m => m.LeftUtc >= startUtc && m.LeftUtc < endUtc &&
                         m.Channel.Administrators.Any(a => a.Id == administratorId),
                    cancellationToken: cancellationToken),
            UnSubscribeCount = await _context.ChannelMembers
                .CountAsync(
                    m => m.JoinedUtc >= startUtc && m.JoinedUtc < endUtc &&
                         m.Channel.Administrators.Any(a => a.Id == administratorId),
                    cancellationToken: cancellationToken)
        };
    }

    public async Task<List<TelegramChannel?>> GetChannels(long administratorId, CancellationToken cancellationToken = default)
    {
        return await _context.Channels
            .Where(c => !c.HasDeleted)
            .Where(c => c.Administrators.Any(a => a.Id == administratorId))
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<TelegramChannel> ActivateRemovedChannel(long userId, long channelId)
    {
        var existChannel = await _context.Channels
            .Include(c => c.Administrators)
            .FirstOrDefaultAsync(c => c.Id == channelId);

        // Если канал ранее удаляли – «воскрешаем» его
        if (existChannel?.HasDeleted == true)
        {
            existChannel.HasDeleted = false;
            _context.Update(existChannel);
            await _context.SaveChangesAsync();
            
            //TODO
            //if (!await CheckIfSheetExists(userId, existChannel.Id))
                //await sheetsService.CreateSheet(existChannel.Id, existChannel.Title, userMessage.From?.Username ?? "stat", State.UserId);
        }

        return existChannel;
    }

    public async Task<TelegramChannel?> GetChannel(long channelId, CancellationToken cancellationToken = default)
    {
        return await _context.Channels
            .Include(q => q.Administrators)
            .FirstOrDefaultAsync(x => x.Id == channelId, cancellationToken: cancellationToken);
    }
    
    public async Task<TelegramChannel?> CreateOrUpdateChannel(
        Chat channelChat, 
        List<TelegramUser> admins,
        TelegramChannel? existChannel = null, CancellationToken cancellationToken = default)
    {
        TelegramChannel? channel;
        if (existChannel != null)
        {
            Console.WriteLine("Updating existing channel.");
            var adminsToKeep = await _context.ChannelAdministrators
                .Where(r => r.ChannelId == existChannel.Id && r.IsManual)
                .Select(r => r.User)
                .ToListAsync(cancellationToken: cancellationToken);

            existChannel.Administrators.Clear();
            foreach (var adm in adminsToKeep)
                existChannel.Administrators.Add(adm);

            foreach (var adm in admins)
            {
                if (existChannel.Administrators.All(a => a.Id != adm.Id))
                {
                    existChannel.Administrators.Add(adm);
                }
            }

            _context.Update(existChannel);
            channel = existChannel;
        }
        else
        {
            Console.WriteLine("Adding new channel to database.");
            channel = new TelegramChannel
            {
                AdPosts = new List<TelegramAdPost>(),
                Administrators = admins,
                Id = channelChat.Id,
                Title = channelChat.Title ?? "",
                Alias = "",
            };
            await _context.Channels.AddAsync(channel, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        
        return channel;
    }

    public async Task<TelegramChannel?> UpdateChannel(TelegramChannel channel, CancellationToken cancellationToken = default)
    {
        var entity = _context.Update(channel);
        await _context.SaveChangesAsync();
        return entity.Entity;
    }

    public async Task<long> GetPendingRequestsCount(long channelId, CancellationToken cancellationToken = default)
    {
        return await _context.ChannelJoinRequests
            .CountAsync(r => r.ChannelId == channelId && r.ApprovedUtc == null && r.DeclinedUtc == null, cancellationToken: cancellationToken);
    }
    /// <summary>
    /// Проверяет, существует ли уже таблица (Sheet) для канала channelId у пользователя userId
    /// </summary>
    private async Task<bool> CheckIfSheetExists(long userId, long channelId)
    {
        var sheetExists = await _context.ChannelSheets
            .Include(s => s.SpreadSheet)
            .Include(s => s.Channel)
            .AnyAsync(s => s.ChannelId == channelId && s.SpreadSheet.UserId == userId);

        return sheetExists;
    }

    public async Task<List<TelegramUser>> GetAdmins(long channelId, CancellationToken cancellationToken)
    {
        return await _context.Channels
            .Where(q => q.Id == channelId)
            .SelectMany(q => q.Administrators)
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveAdmin(long channelId, long adminUserId, CancellationToken cancellationToken = default)
    {
        var channelAdmin = await _context.ChannelAdministrators.FirstOrDefaultAsync(q => q.ChannelId == channelId && q.UsersId == adminUserId, cancellationToken: cancellationToken);
        if (channelAdmin == null)
        {
            return;
        }
        _context.ChannelAdministrators.Remove(channelAdmin);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsAdmin(long channelId, long adminUserId, CancellationToken cancellationToken = default)
    {
        return await _context.ChannelAdministrators.AnyAsync(q => q.ChannelId == channelId && q.UsersId == adminUserId, cancellationToken: cancellationToken);
    }

    public async Task MakeAdmin(long channelId, long adminId, CancellationToken cancellationToken = default)
    {
        await _context.AddAsync(new ChannelAdministrator
        {
            UsersId = adminId,
            ChannelId = channelId,
            IsManual = true,
            User = null,
            Channel = null,
        }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> GetCount(long adminId, CancellationToken cancellationToken = default)
    {
        return await _context.Channels
            .Where(c => !c.HasDeleted)
            .Where(c => c.Administrators.Any(a => a.Id == adminId))
            .CountAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> IsPublic(long chatId, ITelegramBotClient botClient,
        CancellationToken cancellationToken = default)
    {
        var chat = await botClient.GetChatAsync(chatId, cancellationToken: cancellationToken);

        return chat.Type == ChatType.Channel && !string.IsNullOrEmpty(chat.Username);
    }

    public async Task Delete(long channelId, CancellationToken cancellationToken = default)
    {
        var channel = await _context.Channels.FirstOrDefaultAsync(c => c.Id == channelId, cancellationToken);
        if (channel == null)
        {
            return;
        }
        _context.Remove(channel);
        await _context.SaveChangesAsync(cancellationToken);
    }
}