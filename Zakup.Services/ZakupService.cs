using Microsoft.EntityFrameworkCore;
using Zakup.Common.DTO.Zakup;
using Zakup.Entities;
using Zakup.EntityFramework;

namespace Zakup.Services;

public class ZakupService 
{   
    private readonly ApplicationDbContext _context;
    public ZakupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ZakupStatistic> GetStatistics(long userId,DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken = default)
    {
        var zakupQuery = _context.TelegramZakups
            .Where(z => z.Channel.Administrators.Any(a => a.Id == userId))
            .Where(z => z.CreatedUtc >= startUtc && z.CreatedUtc < endUtc);

        var paidQuery = zakupQuery.Where(q => q.IsPad);
        
        return new ZakupStatistic()
        {
            Count = await zakupQuery.CountAsync(cancellationToken),
            Price = await zakupQuery.SumAsync(z => z.Price, cancellationToken),
            Paid = await paidQuery.CountAsync(cancellationToken),
            PaidPrice = await paidQuery.SumAsync(q => q.Price, cancellationToken),
        };
    }

    public async Task<List<TelegramZakup>> GetAdminZakups(string adminKey, long channelId, CancellationToken cancellationToken = default)
    {
        return await _context.TelegramZakups
            .Where(z => z.ChannelId == channelId && z.Admin == adminKey)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<TelegramZakup> Create(TelegramZakup zakup, CancellationToken cancellationToken = default)
    {
        var entity = await _context.TelegramZakups.AddAsync(zakup, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Entity;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var zakup = await _context.TelegramZakups.FindAsync(id, cancellationToken);
        
        _context.Remove(zakup);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TelegramZakup?> Get(Guid zakupId, CancellationToken cancellationToken = default)
    {
        return await _context.TelegramZakups.FindAsync(zakupId, cancellationToken);
    }

    public async Task Update(TelegramZakup zakup, CancellationToken cancellationToken = default)
    {
        _context.Update(zakup);
        await _context.SaveChangesAsync(cancellationToken);
    }
}