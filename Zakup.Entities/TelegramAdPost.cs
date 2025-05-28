using System.ComponentModel.DataAnnotations.Schema;
using Telegram.Bot.Types;
using Zakup.Abstractions.DataContext;

namespace Zakup.Entities;

public class TelegramAdPost : IDeletable
{
    public const string INVITE_URL = "invite.link";

    public Guid Id { get; set; }

    public required string Title { get; set; }

    public required string Text { get; set; }
    
    public required List<MessageEntity> Entities { get; set; }

    public required List<TelegramPostButton> Buttons { get; set; }

    public bool HasDeleted { get; set; }
    public DateTime? DeletedUtc { get; set; }
    
    public required long ChannelId { get; set; }
    public virtual TelegramChannel Channel { get; set; }
    
    public string? MediaGroupId { get; set; }
    public MediaGroup? MediaGroup { get; set; }
}