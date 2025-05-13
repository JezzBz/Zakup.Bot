using Zakup.Common.Enums;

namespace Zakup.Entities;

public class TelegramZakup
{
    public Guid Id { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? PostTime { get; set; }
    
    public bool IsPad { get; set; }
    public string? InviteLink { get; set; }
    public string? Platform { get; set; }
    
    public decimal Price { get; set; }
    
    public bool Accepted { get; set; }
	
    public bool NeedApprove { get; set; }

    public Guid? AdPostId { get; set; }
    
    public TelegramAdPost AdPost { get; set; } = null!;

    public long ChannelId { get; set; }

    public TelegramChannel Channel { get; set; } = null!;

    public IEnumerable<ChannelMember> Members { get; set; } = null!;
    
    public IEnumerable<ZakupClient> Clients { get; set; }
    
    public required ZakupSource ZakupSource { get; set; }
	
	public string? Admin { get; set; }  // Добавлено новое поле

    public bool HasDeleted { get; set; }
    public DateTime? DeletedUtc { get; set; }
}