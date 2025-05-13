using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zakup.Entities;

namespace Zakup.EntityFramework.EntityTypeConfigurations;

public class ChannelFeedbackTypeConfiguration : IEntityTypeConfiguration<ChannelFeedback>
{
    public void Configure(EntityTypeBuilder<ChannelFeedback> builder)
    {
        builder.HasKey(x => x.Id);
    }
}