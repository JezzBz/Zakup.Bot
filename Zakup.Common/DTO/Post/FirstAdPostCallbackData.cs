using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Post;

public class FirstAdPostCallbackData : ICallbackData
{
    public long ChannelId { get; set; }
    
    public bool Create { get; set; }
    public string ToCallback()
    {
        return $"{ChannelId}|{Create}";
    }
    
    public void Parse(List<string> parameters)
    {
        ChannelId = long.Parse(parameters[0]);
        Create = bool.Parse(parameters[1]);
    }
}