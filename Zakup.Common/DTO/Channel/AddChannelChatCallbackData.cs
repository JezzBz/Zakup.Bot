using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Channel;

public class AddChannelChatCallbackData : ICallbackData
{
    public long ChannelId { get; set; }

    public string ToCallback()
    {
        return $"{ChannelId}";
    }
}