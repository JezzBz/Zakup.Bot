using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Channel;

public class AddChannelChatDirectlyCallbackData : ICallbackData
{
    public long ChannelId { get; set; }
    
    public bool Add { get; set; }
    
    public bool RequestFirstPost { get; set; }
    public string ToCallback()
    {
        return $"{ChannelId}|{Add}|{RequestFirstPost}";
    }

    public void Parse(List<string> parameters)
    {
        ChannelId = long.Parse(parameters[0]);
        Add = bool.Parse(parameters[1]);
        RequestFirstPost = bool.Parse(parameters[2]);
    }
}