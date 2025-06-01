using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Channel;

public class DisableAutoApproveCallbackData : ICallbackData
{
    public long ChannelId { get; set; }
    public string ToCallback()
    {
        return $"{ChannelId}";
    }
}