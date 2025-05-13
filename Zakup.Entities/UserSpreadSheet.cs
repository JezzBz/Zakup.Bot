namespace Zakup.Entities;

public class UserSpreadSheet
{
    public string Id { get; set; }
    
    public long UserId { get; set; }
    
    public TelegramUser User { get; set; }
}