using Microsoft.EntityFrameworkCore;
using Zakup.Common.DTO.Channel;
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
}