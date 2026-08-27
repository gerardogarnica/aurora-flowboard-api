using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.Shared;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class FlowStateConfiguration : IEntityTypeConfiguration<FlowState>
{
    public void Configure(EntityTypeBuilder<FlowState> builder)
    {
        builder.ToTable("flow_states");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProjectId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(FlowState.MaxNameLength);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.OwnsOne(x => x.Color, color =>
        {
            color.Property(c => c.Value)
                .HasColumnName("color")
                .IsRequired()
                .HasMaxLength(Color.MaxLength);
        });

        builder.HasIndex(x => x.ProjectId);
    }
}
