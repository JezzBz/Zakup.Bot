using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class TelegramUserTypeConfiguration : IEntityTypeConfiguration<TelegramUser>
{
    public void Configure(EntityTypeBuilder<TelegramUser> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasMany(x => x.Channels)
            .WithMany(x => x.Administrators)
            .UsingEntity<ChannelAdministrator>();
    }
}