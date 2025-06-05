using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Zakup;

public class AddZakupDateCallbackData : ICallbackData
{
    public long ChannelId { get; set; }
    public string ToCallback()
    {
        return $"{ChannelId}";
    }
    
    public void Parse(List<string> parameters)
    {
        ChannelId = long.Parse(parameters[0]);
    }
}