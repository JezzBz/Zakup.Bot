using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class ChannelJoinRequestTypeConfiguration : IEntityTypeConfiguration<ChannelJoinRequest>
{
    public void Configure(EntityTypeBuilder<ChannelJoinRequest> builder)
    {
       builder.HasKey(x => x.Id);

       builder.Property(x => x.InviteLink)
           .HasMaxLength(1024);
    }
}