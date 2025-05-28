using Zakup.Common.Enums;

public class TelegramDocument
{
    public Guid Id { get; set; }

    public required string FileId { get; set; }
    public TelegramDocumentKind Kind { get; set; }
    
    public string? ThumbnailId { get; set; }
    
}