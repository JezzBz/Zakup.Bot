namespace Zakup.Common.DTO.Zakup;

public class PlacementStatisticDTO
{
    public DateTime PlaceDate { get; set; }
    
    public string Platform { get; set; }
    
    public decimal Price { get; set; }
    
    public long TotalSubscribers { get; set; }
    public long RemainingSubscribers { get; set; }
    public long ClientsCount { get; set; }
    public long CommentersCount { get; set; }
    
    
    public long ChannelId { get; set; }
}