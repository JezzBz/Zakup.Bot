using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Zakup;

public class PDPVerificationCallbackData : ICallbackData
{
    public long RequestUserId { get; set; }
    
    public long ChannelId { get; set; }
    
    public Guid PlacementId { get; set; }
    public string ToCallback()
    {
        return $"{RequestUserId}|{ChannelId}|{PlacementId}";
    }
}