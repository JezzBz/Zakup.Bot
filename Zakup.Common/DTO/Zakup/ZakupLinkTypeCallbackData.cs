using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Zakup;

public class ZakupLinkTypeCallbackData : ICallbackData
{
    public Guid ZakupId { get; set; }
    
    public bool Private { get; set; }
    public string ToCallback()
    {
        
        return $"{ZakupId}|{Private}";
    }
}