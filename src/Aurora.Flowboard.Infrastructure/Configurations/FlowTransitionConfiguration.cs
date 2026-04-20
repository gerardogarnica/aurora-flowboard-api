using Aurora.Flowboard.Domain.Flows;
using Aurora.Flowboard.Domain.Projects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class FlowTransitionConfiguration : IEntityTypeConfiguration<FlowTransition>
{
    public void Configure(EntityTypeBuilder<FlowTransition> builder)
    {
        builder.ToTable("flow_transitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FlowId)
            .IsRequired();

        builder.Property(x => x.FromStateId)
            .IsRequired();

        builder.Property(x => x.ToStateId)
            .IsRequired();

        builder.PrimitiveCollection<ProjectRole>("_allowedRoles")
            .HasColumnName("allowed_roles")
            .IsRequired();

        builder.HasOne<FlowState>()
            .WithMany()
            .HasForeignKey(x => x.FromStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FlowState>()
            .WithMany()
            .HasForeignKey(x => x.ToStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.FlowId);

        builder.HasIndex(x => new { x.FlowId, x.FromStateId, x.ToStateId })
            .IsUnique();
    }
}
