using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class MessageForwardTypeConfiguration : IEntityTypeConfiguration<MessageForward>
{
    public void Configure(EntityTypeBuilder<MessageForward> builder)
    {
        builder.HasKey(x => x.Id);
    }
}