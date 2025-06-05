using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Channel;

public class ChannelStatisticCallbackData : ICallbackData
{
    public long ChannelId { get; set; }

    public int PageNumber { get; set; } = 1;
    public string ToCallback()
    {
        return $"{ChannelId}|{PageNumber}";
    }
    
    public void Parse(List<string> parameters)
    {
        ChannelId = long.Parse(parameters[0]);
        PageNumber = int.Parse(parameters[1]);
    }
}