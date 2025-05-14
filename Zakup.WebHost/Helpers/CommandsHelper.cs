using Zakup.Common.DTO;
using Zakup.Common.Enums;

namespace Zakup.WebHost.Helpers;

public static class CommandsHelper
{
    public static CallbackInfo ParseCallback(string command)
    {
        var result  = new CallbackInfo();
        var data = command.Split("|");
        result.Command = Enum.Parse<CallbackType>(data[0]);
        result.Params = data.Skip(1);
        return result;
    }
    
    /// <summary>
    /// Для команд формата {command value}
    /// </summary>
    /// <param name="input"></param>
    /// <param name="command"></param>
    /// <returns></returns>
    public static string? ParseCommandValue(string input, string command)
    {
        if (input.StartsWith(command))
        {
            return command.Length == input.Length ? null : input.Substring(command.Length + 1);
        }
        return input;
    }
}