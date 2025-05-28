using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Channel;

public class ChannelAutoApproveCallbackData : ICallbackData
{
    public long ChannelId { get; set; }
    public string ToCallback()
    {
        return $"{ChannelId}";
    }
}