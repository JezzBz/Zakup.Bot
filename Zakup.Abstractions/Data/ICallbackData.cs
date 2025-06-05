namespace Zakup.Abstractions.Data;

public interface ICallbackData
{
    string ToCallback();

    void Parse(List<string> parameters);
}