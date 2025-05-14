using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Zakup.Common.Enums;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class TelegramZakupTypeConfiguration : IEntityTypeConfiguration<TelegramZakup>
{
    public void Configure(EntityTypeBuilder<TelegramZakup> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.AdPost)
            .WithMany()
            .HasForeignKey(x => x.AdPostId);
        
        builder.HasOne(x => x.Channel)
            .WithMany()
            .HasForeignKey(x => x.ChannelId);
        
        builder.Property(x => x.InviteLink)
            .HasMaxLength(1024);

        builder.Property(x => x.Platform)
            .HasMaxLength(512);
        
        builder.Property(x => x.Admin)
            .HasMaxLength(255);
        
        builder.Property(x => x.ZakupSource)
            .HasConversion(new EnumToStringConverter<ZakupSource>());
    }
}