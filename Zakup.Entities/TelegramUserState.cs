using Zakup.Common.Enums;

namespace Zakup.Entities;

public class TelegramUserState
{
    public Guid Id { get; set; }
    
    public long UserId { get; set; }
    
    public virtual required TelegramUser User { get; set; }
    
    public  UserStateType State { get; set; }
    
    public string? CachedValue { get; set; }
    
    public int PreviousMessageId { get; set; }
    
    public int? MenuMessageId { get; set; }
}