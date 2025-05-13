using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class TelegramChannelTypeConfiguration : IEntityTypeConfiguration<TelegramChannel>
{
    public void Configure(EntityTypeBuilder<TelegramChannel> builder)
    {
        builder.HasKey(x => x.Id);
        
        
        builder.Property(x => x.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Alias)
            .HasMaxLength(255);
        
        builder.HasMany(x => x.AdPosts)
            .WithOne(x => x.Channel)
            .HasForeignKey(x => x.ChannelId);
        
        builder.HasMany(x => x.JoinRequests)
            .WithOne(x => x.Channel)
            .HasForeignKey(x => x.ChannelId);
        
        builder.HasMany(x => x.Members)
            .WithOne(x => x.Channel)
            .HasForeignKey(x => x.ChannelId);
    }
}