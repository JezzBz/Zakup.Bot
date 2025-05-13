using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class ChannelSheetTypeConfiguration : IEntityTypeConfiguration<ChannelSheet>
{
    public void Configure(EntityTypeBuilder<ChannelSheet> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.SpreadSheet)
            .WithMany()
            .HasForeignKey(x => x.SpreadSheetId);
        
        builder.HasOne(x => x.Channel)
            .WithMany()
            .HasForeignKey(x => x.ChannelId);
    }
}