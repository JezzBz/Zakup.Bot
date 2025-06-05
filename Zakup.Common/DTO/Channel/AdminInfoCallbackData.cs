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
    
    public void Parse(List<string> parameters)
    {
        UserId = long.Parse(parameters[0]);
        ChannelId = long.Parse(parameters[1]);
    }
}