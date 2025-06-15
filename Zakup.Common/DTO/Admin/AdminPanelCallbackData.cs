using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Admin;

public class AdminPanelCallbackData : ICallbackData
{
    public string ToCallback() => string.Empty;
    public void Parse(List<string> parameters) { }
} 