using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Admin;

public class AdminUnscamCallbackData : ICallbackData
{
    public string ToCallback()
    {  
        return string.Empty;
    }

    public void Parse(List<string> parameters)
    {
    }
} 