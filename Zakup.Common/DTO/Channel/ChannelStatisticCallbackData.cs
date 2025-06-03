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
}