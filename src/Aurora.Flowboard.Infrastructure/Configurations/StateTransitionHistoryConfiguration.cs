using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.WorkItems;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class StateTransitionHistoryConfiguration : IEntityTypeConfiguration<StateTransitionHistory>
{
    public void Configure(EntityTypeBuilder<StateTransitionHistory> builder)
    {
        builder.ToTable("work_item_state_transition_histories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WorkItemId)
            .IsRequired();

        builder.Property(x => x.FromStateId);

        builder.Property(x => x.ToStateId)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(StateTransitionHistory.MaxReasonLength);

        builder.Property(x => x.ChangedById)
            .IsRequired();

        builder.Property(x => x.ChangedOnUtc)
            .IsRequired();

        builder.HasOne<FlowState>()
            .WithMany()
            .HasForeignKey(x => x.FromStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FlowState>()
            .WithMany()
            .HasForeignKey(x => x.ToStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.WorkItemId);
    }
}
