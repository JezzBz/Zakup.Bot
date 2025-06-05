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
}