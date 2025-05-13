using Zakup.Common.DTO;
using Zakup.Common.Enums;

namespace Zakup.WebHost.Helpers;

public static class CallbackCommandsHelper
{
    public static CallbackInfo Parse(string command)
    {
        var result  = new CallbackInfo();
        var data = command.Split("|");
        result.Command = Enum.Parse<CallbackType>(data[0]);
        result.Params = data.Skip(1);
        return result;
    }
}