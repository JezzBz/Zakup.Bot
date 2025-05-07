using Zakup.Common.Enums;

namespace Zakup.Entities;

public class MessageForward
{
    public long Id { get; set; }
    
    public required long UserId { get; set; }
    
    public DateTime ForwardAtUtc { get; set; }
    
    public MessageForwardSource Source { get; set; }
}