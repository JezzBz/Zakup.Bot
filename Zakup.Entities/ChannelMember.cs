namespace Zakup.Entities;

public class ChannelMember
{
    public long Id { get; set; }

    public required long UserId { get; set; }

    public bool? IsPremium { get; set; }
	
    public bool? IsCommenter { get; set; }
    
    public string? UserName { get; set; } 
    
    public required long ChannelId { get; set; }

    public TelegramChannel Channel { get; set; } = null!;

    public bool Status { get; set; }

    public string? InviteLink { get; set; }
    
    public string? InviteLinkName { get; set; }

    public Guid? ZakupId { get; set; }
    public TelegramZakup? Zakup { get; set; } = null!;


    public required string Refer { get; set; }
    public int JoinCount { get; set; }

    public DateTime? JoinedUtc { get; set; }

    public DateTime? LeftUtc { get; set; }
}