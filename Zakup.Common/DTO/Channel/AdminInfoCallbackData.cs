using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Channel;

public class AdminInfoCallbackData : ICallbackData
{
    public long UserId { get; set; }
    public long ChannelId { get; set; }
    public string ToCallback()
    {
        return $"{UserId}|{ChannelId}";
    }
}