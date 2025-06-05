using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO;

public class EmptyCallbackData: ICallbackData
{
    public string ToCallback()
    {
        return string.Empty;
    }

    public void Parse(List<string> parameters)
    {
    }
}