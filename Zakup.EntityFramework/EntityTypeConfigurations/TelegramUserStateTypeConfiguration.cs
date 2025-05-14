using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Zakup.Common.Enums;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class TelegramUserStateTypeConfiguration : IEntityTypeConfiguration<TelegramUserState>
{
    public void Configure(EntityTypeBuilder<TelegramUserState> builder)
    {
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.State)
            .HasConversion(new EnumToStringConverter<UserStateType>());
        
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId);
    }
}