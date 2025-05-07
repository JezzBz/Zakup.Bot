using Zakup.Abstractions.Data;
using Zakup.Common.Enums;

namespace Zakup.Common.Models;

public class RateChannelCallbackData : ICallbackData
{
    public ChannelRateType RateType { get; set; }
    
    public long ChannelId { get; set; }

    public string ToCallback()
    {
        return $"{RateType}|{ChannelId}";
    }
}