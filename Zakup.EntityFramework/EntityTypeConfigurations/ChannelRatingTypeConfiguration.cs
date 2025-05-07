using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class ChannelRatingTypeConfiguration : IEntityTypeConfiguration<ChannelRating>
{
    public void Configure(EntityTypeBuilder<ChannelRating> builder)
    {
        builder.HasKey(q => q.ChannelId);
    }
}