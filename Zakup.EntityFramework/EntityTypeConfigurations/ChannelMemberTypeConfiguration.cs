using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class ChannelMemberTypeConfiguration : IEntityTypeConfiguration<ChannelMember>
{
    public void Configure(EntityTypeBuilder<ChannelMember> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.Zakup)
            .WithMany()
            .HasForeignKey(x => x.ZakupId);
        
        builder
            .Property(x => x.UserName)
            .HasMaxLength(500);

        builder.Property(x => x.InviteLink)
            .HasMaxLength(1024);
        
        builder.Property(x => x.InviteLinkName)
            .HasMaxLength(255);
    }
}