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
    
    public void Parse(List<string> parameters)
    {
        ZakupId = Guid.Parse(parameters[0]);
        Private = bool.Parse(parameters[1]);
    }
}