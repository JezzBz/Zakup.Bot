using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Zakup;

public class ChangePriceCallbackData : ICallbackData
{
    public Guid ZakupId { get; set; }
    public string ToCallback()
    {
        return $"{ZakupId}";
    }
}