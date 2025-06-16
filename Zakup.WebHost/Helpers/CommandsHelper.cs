using System.Text.RegularExpressions;
using Zakup.Common.DTO;
using Zakup.Common.Enums;

namespace Zakup.WebHost.Helpers;

public static class CommandsHelper
{
    public static CallbackInfo ParseCallback(string command)
    {
        var parsed = Enum.TryParse<CallbackType>(command, out var type);

        if (parsed)
        {
            return new CallbackInfo()
            {
                Command = type,
                Params = new List<string>()
            };
        }
        
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
    
    public static string EscapeMarkdownV2(string text)
    {
        if (text == null) return "";
        
        // Сначала экранируем обратные слеши, чтобы при замене других символов они не потерялись
        text = text.Replace("\\", "\\\\");
        
        var charactersToEscape = new char[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        foreach (var character in charactersToEscape)
        {
            text = text.Replace(character.ToString(), "\\" + character);
        }
        return text;
    }

    public static int WordsCount(string? input)
    {
        if (input is null)
        {
            return 0;
        }
        
        char[] delimiters = new char[] {' ', '\r', '\n', '\t'};
        return input.Split(delimiters,StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public static bool IsNickname(string text)
    {
        var regex = new Regex(@"^@[A-Za-z0-9_]{5,32}$");
        return regex.IsMatch(text);
    }
}