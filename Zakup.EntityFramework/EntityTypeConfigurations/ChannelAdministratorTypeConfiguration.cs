using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class ChannelAdministratorTypeConfiguration : IEntityTypeConfiguration<ChannelAdministrator>
{
    public void Configure(EntityTypeBuilder<ChannelAdministrator> builder)
    {
        builder.Property(p => p.Version)
            .IsRowVersion();

        builder.HasOne(q => q.Channel)
            .WithMany()
            .HasForeignKey(q => q.ChannelId);

        builder.HasOne(q => q.User)
            .WithMany()
            .HasForeignKey(q => q.UsersId);
    }
}