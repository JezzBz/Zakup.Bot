using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Admin;

public class AdminConfirmScamCallbackData : ICallbackData
{
    public long ChannelId { get; set; }
    public string ToCallback()
    {
        return ChannelId.ToString();
    }

    public void Parse(List<string> parameters)
    {
        ChannelId = long.Parse(parameters[0]);
    }
}