using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class MediaGroupTypeConfiguration : IEntityTypeConfiguration<MediaGroup>
{
    public void Configure(EntityTypeBuilder<MediaGroup> builder)
    {
        builder.HasKey(mg => mg.MediaGroupId);

        builder.HasMany(q => q.Documents)
            .WithMany()
            .UsingEntity<FileMediaGroup>();
    }
}