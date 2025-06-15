using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Admin;

public class AdminScamCallbackData : ICallbackData
{
    public string ToCallback()
    {  
        return string.Empty;
    }

    public void Parse(List<string> parameters)
    {
    }
} 