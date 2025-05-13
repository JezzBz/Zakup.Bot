namespace Zakup.Entities;

public class ChannelSheet
{
    public int Id { get; set; }
    
    public string SpreadSheetId { get; set; }
    
    public UserSpreadSheet SpreadSheet { get; set; }
    
    public long ChannelId { get; set; }
    
    public TelegramChannel Channel { get; set; }
}