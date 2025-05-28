using Zakup.Abstractions.Data;

namespace Zakup.Common.DTO.Post;

public class AddPostButtonCallbackData : ICallbackData
{
    public Guid AdPostId { get; set; }
    
    public bool Add { get; set; }
    public string ToCallback()
    {

        return $"{AdPostId}|{Add}";
    }
}