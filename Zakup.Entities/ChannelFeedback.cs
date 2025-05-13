namespace Zakup.Entities;

public class ChannelFeedback
{
    public long Id { get; set; }
    
    public required long FromUserId { get; set; }

    public TelegramUser FromUser { get; set; } = null!;
    
    public required long ChannelId { get; set; }

    public bool Positive { get; set; }
    
    public DateTime CreatedUtc { get; set; }
}