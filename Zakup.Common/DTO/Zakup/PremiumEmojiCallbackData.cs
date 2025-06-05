using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Zakup;

public class PremiumEmojiCallbackData : ICallbackData
{
    public Guid ZakupId { get; set; }
    public Guid AdPostId { get; set; }
    public string ToCallback()
    {
        return $"{ZakupId}|{AdPostId}";
    }
    
    public void Parse(List<string> parameters)
    {
            ZakupId = Guid.Parse(parameters[0]);
            AdPostId = Guid.Parse(parameters[1]);
    }
}