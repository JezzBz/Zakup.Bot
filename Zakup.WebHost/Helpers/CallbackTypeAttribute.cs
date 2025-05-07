using Zakup.Common.Enums;

namespace Zakup.WebHost.Helpers;

[AttributeUsage(AttributeTargets.Class)]
public class CallbackTypeAttribute : Attribute
{
    public CallbackType Type { get; }
    public CallbackTypeAttribute(CallbackType type) => Type = type;
}