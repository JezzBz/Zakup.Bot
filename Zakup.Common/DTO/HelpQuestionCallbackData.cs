using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO;

public class HelpQuestionCallbackData : ICallbackData
{
    public int QuestionId { get; set; }
    public string ToCallback()
    {
        return $"{QuestionId}";
    }
    
    public void Parse(List<string> parameters)
    {
        QuestionId = int.Parse(parameters[0]);
    }
}