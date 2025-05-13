using Zakup.Common.Enums;

namespace Zakup.WebHost.Helpers;

[AttributeUsage(AttributeTargets.Class)]
public class StateTypeAttribute  : Attribute
{
    public UserStateType State { get; }
    public StateTypeAttribute(UserStateType state) => State = state;
}