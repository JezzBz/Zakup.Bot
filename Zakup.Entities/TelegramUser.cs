namespace Zakup.Entities;

public class TelegramUser
{
    public long Id { get; set; }
    
    public string? UserName { get; set; }
    
    public string? Refer { get; set; }
    
    public DateTime? MutedToUtc { get; set; }
    
    public Guid? UserStateId { get; set; }
    
    public virtual TelegramUserState UserState { get; set; }
    
    public virtual required IEnumerable<TelegramChannel> Channels { get; set; }
}