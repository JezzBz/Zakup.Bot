using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class TelegramAdPostTypeConfiguration : IEntityTypeConfiguration<TelegramAdPost>
{
    public void Configure(EntityTypeBuilder<TelegramAdPost> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.File)
            .WithMany()
            .HasForeignKey(x => x.FileId);
        
        builder.HasOne(x => x.Channel)
            .WithMany()
            .HasForeignKey(x => x.ChannelId);
        
        builder.Property(x => x.Text)
            .HasMaxLength(4096)
            .IsRequired();
        
        builder.Property(x => x.Title)
            .HasMaxLength(255)
            .IsRequired();
        
        builder.Property(x => x.Buttons)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<TelegramPostButton>>(v)!
            );
        
        builder.Property(e => e.Entities)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<MessageEntity>>(v)!
            );

        builder.HasQueryFilter(r => !r.HasDeleted);
    }
}