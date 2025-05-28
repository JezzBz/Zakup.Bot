namespace Zakup.Entities;

public class FileMediaGroup
{
    public Guid FileId { get; set; }
    
    public TelegramDocument File { get; set; }
    
    public string MediaGroupId { get; set; }
    
    public MediaGroup MediaGroup { get; set; }
}