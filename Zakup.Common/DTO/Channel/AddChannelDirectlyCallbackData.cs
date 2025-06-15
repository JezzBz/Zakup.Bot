using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Channel;

public class AddChannelDirectlyCallbackData : ICallbackData
{
    public long ChannelId { get; set; }
    public string ChannelTitle { get; set; }
    public string ToCallback()
    {
        return $"{ChannelId}|{ChannelTitle}";
    }

    public void Parse(List<string> parameters)
    {
        ChannelId = long.Parse(parameters[0]);
        ChannelTitle = parameters[1];
    }
}