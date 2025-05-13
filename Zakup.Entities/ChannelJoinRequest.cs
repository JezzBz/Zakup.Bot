namespace Zakup.Entities;

public class ChannelJoinRequest
{
    public long Id { get; set; }

    public required long UserId { get; set; }

    public required long ChannelId { get; set; }
    
    public TelegramChannel Channel { get; set; }

    public string? InviteLink { get; set; } 

    public DateTime? ApprovedUtc { get; set; }
    
    public DateTime? DeclinedUtc { get; set; }
    
    public DateTime? RequestedUtc { get; set; }
}