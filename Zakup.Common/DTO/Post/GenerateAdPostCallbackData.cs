using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Post;

public class GenerateAdPostCallbackData : ICallbackData
{
    public Guid PostId { get; set; }
    public string ToCallback()
    {
        return $"{PostId}";
    }
    
    public void Parse(List<string> parameters)
    {
        PostId = Guid.Parse(parameters[0]);
    }
}