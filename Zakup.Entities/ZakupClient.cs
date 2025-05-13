namespace Zakup.Entities;

public class ZakupClient
{
    public long Id { get; set; }
    public Guid ZakupId { get; set; }
    
    public TelegramZakup Zakup { get; set; }
    
    public long MemberId { get; set; }
    
    public ChannelMember Member { get; set; }
}