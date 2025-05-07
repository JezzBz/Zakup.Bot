using Zakup.Common.Enums;

namespace Zakup.Common.DTO;

public class CallbackInfo
{
    public CallbackType Command { get; set; }
    
    public IEnumerable<string> Paramers { get; set; }
}