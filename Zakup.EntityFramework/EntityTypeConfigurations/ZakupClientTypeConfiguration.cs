using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class ZakupClientTypeConfiguration : IEntityTypeConfiguration<ZakupClient>
{
    public void Configure(EntityTypeBuilder<ZakupClient> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.Zakup)
            .WithMany(x => x.Clients)
            .HasForeignKey(x => x.ZakupId);
        
        builder.HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => x.MemberId);
        
    }
}