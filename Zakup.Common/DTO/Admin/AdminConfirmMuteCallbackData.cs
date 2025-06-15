using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Admin;

public class AdminConfirmMuteCallbackData : ICallbackData
{
    public long UserId { get; set; }
    public string ToCallback()
    {
        return UserId.ToString();
    }

    public void Parse(List<string> parameters)
    {
        UserId = long.Parse(parameters[0]);
    }
}