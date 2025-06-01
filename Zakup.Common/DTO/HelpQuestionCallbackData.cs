using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO;

public class HelpQuestionCallbackData : ICallbackData
{
    public int QuestionId { get; set; }
    public string ToCallback()
    {
        return $"{QuestionId}";
    }
}