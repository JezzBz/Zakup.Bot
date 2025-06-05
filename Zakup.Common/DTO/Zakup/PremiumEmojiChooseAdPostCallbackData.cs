using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Zakup;

public class PremiumEmojiChooseAdPostCallbackData : ICallbackData
{
    public Guid ZakupId { get; set; }
    public string ToCallback()
    {
        return $"{ZakupId}";
    }
    
    public void Parse(List<string> parameters)
    {
        ZakupId = Guid.Parse(parameters[0]);
    }
}