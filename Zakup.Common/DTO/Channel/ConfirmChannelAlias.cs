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
}