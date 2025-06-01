using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Zakup;

public class ZakupCheckChannelPrivateCallbackData : ICallbackData
{
    public Guid ZakupId { get; set; }
    public string ToCallback()
    {
       return ZakupId.ToString();
    }
}