namespace Zakup.Entities;

public class ChannelAdministrator
{
    public long UsersId { get; set; }
    public virtual required TelegramUser User { get; set; }

    public long ChannelId { get; set; }
    public virtual required TelegramChannel Channel { get; set; }
	
    public bool IsManual { get; set; } // Новое свойство
    
    public byte[]? Version { get; set; }
}