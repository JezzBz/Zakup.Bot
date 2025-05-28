namespace Zakup.Entities;

public class MediaGroup
{
    public string MediaGroupId { get; set; }
    
    public IEnumerable<TelegramDocument> Documents { get; set; }
}