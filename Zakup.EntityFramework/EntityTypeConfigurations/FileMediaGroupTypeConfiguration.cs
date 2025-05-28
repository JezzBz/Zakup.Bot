using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class FileMediaGroupTypeConfiguration : IEntityTypeConfiguration<FileMediaGroup>
{
    public void Configure(EntityTypeBuilder<FileMediaGroup> builder)
    {
        builder.HasKey(x => new {x.FileId, x.MediaGroupId});
    }
}