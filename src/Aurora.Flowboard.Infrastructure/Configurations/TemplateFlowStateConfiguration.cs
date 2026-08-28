using Aurora.Flowboard.Domain.Shared;
using Aurora.Flowboard.Domain.TemplateFlows;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class TemplateFlowStateConfiguration : IEntityTypeConfiguration<TemplateFlowState>
{
    public void Configure(EntityTypeBuilder<TemplateFlowState> builder)
    {
        builder.ToTable("template_flow_states");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateFlowId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(TemplateFlowState.MaxNameLength);

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

        builder.HasIndex(x => x.TemplateFlowId);
    }
}
