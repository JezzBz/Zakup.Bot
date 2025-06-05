using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Channel;

public class ConfirmChannelAlias : ICallbackData
{
    public string Alias { get; set; }

    public long ChannelId { get; set; }
    public bool Confirm { get; set; }
    public bool RequestFirstPost { get; set; }

    public string ToCallback()
    {
        return $"{Alias}|{ChannelId}|{Confirm}|{RequestFirstPost}";
    }
    
    public void Parse(List<string> parameters)
    {
        if (parameters.Count < 2)
        {
            throw new Exception();
        }
        
        Alias = parameters[0];
        ChannelId = long.Parse(parameters[1]);
        Confirm = bool.Parse(parameters[2]);
        RequestFirstPost = bool.Parse(parameters[3]);
    }
}