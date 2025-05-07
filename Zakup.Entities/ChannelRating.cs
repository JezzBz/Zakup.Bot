namespace Zakup.Entities;

public class ChannelRating
{
    public required long ChannelId { get; set; }
        
    public long BadDeals { get; set; }
        
    public long Rate { get; set; }
}