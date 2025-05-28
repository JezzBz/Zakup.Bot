using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Post;

public class DeleteAdPostCallbackData : ICallbackData
{
    
    public Guid PostId { get; set; }
    
    public string ToCallback()
    {
        return $"{PostId}";
    }
}