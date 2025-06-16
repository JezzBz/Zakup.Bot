using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class AnalyzePointsBalanceTypeConfiguration : IEntityTypeConfiguration<AnalyzePointsBalance>
{
    public void Configure(EntityTypeBuilder<AnalyzePointsBalance> builder)
    {
        builder.HasKey(x => x.UserId);

        builder
            .Property(x => x.Balance)
            .HasDefaultValue(0);

    }
}