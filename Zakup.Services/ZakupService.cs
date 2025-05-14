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
}