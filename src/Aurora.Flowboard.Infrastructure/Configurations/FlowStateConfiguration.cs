using Aurora.Flowboard.Domain.Flows;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class FlowStateConfiguration : IEntityTypeConfiguration<FlowState>
{
    public void Configure(EntityTypeBuilder<FlowState> builder)
    {
        builder.ToTable("flow_states");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FlowId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.Category)
            .IsRequired()
            .HasConversion<string>();

        builder.HasIndex(x => x.FlowId);
    }
}
