using Zakup.Abstractions.DataContext;

namespace Zakup.Entities;

public class TelegramChannel : IDeletable
{
    public required long Id { get; set; }
    public required string Title { get; set; }

    public long? MinutesToAcceptRequest { get; set; }
    public string? Alias { get; set; }

    public virtual required List<TelegramUser> Administrators { get; set; }
    
    public IEnumerable<ChannelJoinRequest> JoinRequests { get; set; }
    
    public IEnumerable<ChannelMember> Members { get; set; }
    public virtual required IEnumerable<TelegramAdPost> AdPosts { get; set; }
    
    public long? ChannelChatId { get; set; }
    
    public bool HasDeleted { get; set; }
    public DateTime? DeletedUtc { get; set; }
}