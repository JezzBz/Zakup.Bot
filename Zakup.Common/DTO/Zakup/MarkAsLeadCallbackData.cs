using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Zakup;

public class MarkAsLeadCallbackData : ICallbackData
{
    public long LeadUserId { get; set; }
    public string ToCallback()
    {
        return $"{LeadUserId}";
    }
}