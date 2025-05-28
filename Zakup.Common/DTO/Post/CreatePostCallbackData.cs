using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Post;

public class CreatePostCallbackData : ICallbackData
{
    public long ChannelId { get; set; }
    public string ToCallback()
    {
        return $"{ChannelId}";
    }
}