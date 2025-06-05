using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Channel;

public class RemoveAdminCallbackData : ICallbackData
{
    public long ChannelId { get; set; }
    public long AdminUserId { get; set; }
    public string ToCallback()
    {
        return $"{ChannelId}|{AdminUserId}";
    }
    
    public void Parse(List<string> parameters)
    {
        ChannelId = long.Parse(parameters[0]);
        AdminUserId = long.Parse(parameters[1]);
    }
}