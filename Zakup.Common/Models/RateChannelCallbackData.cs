using Zakup.Abstractions.Data;
using Zakup.Common.Enums;

namespace Zakup.Common.Models;

/// <summary>
/// Команда "Оценка канала"
/// </summary>
public class RateChannelCallbackData : ICallbackData
{
    public ChannelRateType RateType { get; set; }
    
    public long ChannelId { get; set; }

    public string ToCallback()
    {
        return $"{RateType}|{ChannelId}";
    }
    
    public void Parse(List<string> parameters)
    {
        RateType = Enum.Parse<ChannelRateType>(parameters[0]);
        ChannelId = long.Parse(parameters[1]);
    }
}