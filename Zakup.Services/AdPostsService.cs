using Microsoft.EntityFrameworkCore;
using Zakup.Entities;
using Zakup.EntityFramework;

namespace Zakup.Services;

public class AdPostsService
{
    private readonly ApplicationDbContext _context;

    public AdPostsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TelegramAdPost> SavePost(TelegramAdPost post)
    {
       var entity = await _context.TelegramAdPosts.AddAsync(post);
       await _context.SaveChangesAsync();
       
       return entity.Entity;
    }

    public async Task<IEnumerable<TelegramAdPost>> GetPosts(long channelId, long userId, CancellationToken cancellationToken = default)
    {
        return await _context.TelegramAdPosts
            .Where(q => q.ChannelId == channelId)
            .Where(q => q.Channel.Administrators.Any(x => x.Id == userId))
            .ToListAsync(cancellationToken: cancellationToken);
    }
    
    public async Task<List<TelegramAdPost>> GetPosts(long channelId, CancellationToken cancellationToken = default)
    {
        return await _context.TelegramAdPosts
            .Where(q => q.ChannelId == channelId)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task AddButton(Guid postId, TelegramPostButton button)
    {
        var adPost = await _context.TelegramAdPosts.FirstAsync(a => a.Id == postId);

        adPost.Buttons.Add(button);

        _context.Update(adPost);
        await _context.SaveChangesAsync();
    }

    public async Task<TelegramAdPost?> Get(Guid postId, CancellationToken token = default, bool  includeAll = false)
    {
        var query = _context.TelegramAdPosts.AsQueryable();

        if (includeAll)
        {
            query = query
                    .Include(q => q.MediaGroup)
                    .ThenInclude(q => q!.Documents);
        }
        
        return await 
            query
            .FirstOrDefaultAsync(a => a.Id == postId, cancellationToken: token); 
    }

    public async Task UpdatePost(TelegramAdPost post)
    {
        _context.Update(post);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(Guid postId, CancellationToken token = default)
    {
        var post = await _context.TelegramAdPosts
            .FirstOrDefaultAsync(a => a.Id == postId, cancellationToken: token);
        
        if (post == null)
        {
            return;
        }

        _context.TelegramAdPosts.Remove(post);
        await _context.SaveChangesAsync(token);
    }
    
    public async Task Delete(TelegramAdPost post, CancellationToken token = default)
    {
        if (post == null)
        {
            return;
        }

        _context.TelegramAdPosts.Remove(post);
        await _context.SaveChangesAsync(token);
    }
}