using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Zakup.Common.Enums;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class TelegramDocumentTypeConfiguration : IEntityTypeConfiguration<TelegramDocument>
{
    public void Configure(EntityTypeBuilder<TelegramDocument> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder .Property(x => x.Kind)
            .HasConversion(new EnumToStringConverter<TelegramDocumentKind>());
    }
}