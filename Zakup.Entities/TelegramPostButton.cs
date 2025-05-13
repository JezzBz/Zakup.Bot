namespace Zakup.Entities;

public class TelegramPostButton
{
    public required string Text { get; set; }
    public string? Url { get; set; }


    public bool HasRequireReplaceToInviteLink() => Url?.Contains(TelegramAdPost.INVITE_URL) ?? false;
}