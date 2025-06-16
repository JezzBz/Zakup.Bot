namespace Zakup.Entities;

public class ChannelsAnalyzeProcess
{
    public Guid Id { get; set; }
    
    public string AnalyzeTarget { get; set; }
    
    public long UserId { get; set; }
    
    public TelegramUser User { get; set; }
}