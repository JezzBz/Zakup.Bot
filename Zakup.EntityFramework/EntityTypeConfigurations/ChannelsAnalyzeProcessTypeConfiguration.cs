using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class ChannelsAnalyzeProcessTypeConfiguration : IEntityTypeConfiguration<ChannelsAnalyzeProcess>
{
    public void Configure(EntityTypeBuilder<ChannelsAnalyzeProcess> builder)
    {
        builder.HasKey(q => q.Id);
        
        builder.HasOne(q => q.User)
            .WithMany()
            .HasForeignKey(q => q.UserId);
        
        builder.Property(q => q.Success)
            .HasDefaultValue(null);
    }
}