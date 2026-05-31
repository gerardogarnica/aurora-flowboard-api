using Aurora.Flowboard.Domain.Flows;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class FlowConfiguration : IEntityTypeConfiguration<Flow>
{
    public void Configure(EntityTypeBuilder<Flow> builder)
    {
        builder.ToTable("flows");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Flow.MaxNameLength);

        builder.Property(x => x.Description)
            .HasMaxLength(Flow.MaxDescriptionLength);

        builder.Property(x => x.ProjectId)
            .IsRequired();

        builder.Property(x => x.IsDefault)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc);

        builder.HasOne<Project>(x => x.Project)
            .WithMany(p => p.Flows)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.States)
            .WithOne(x => x.Flow)
            .HasForeignKey(x => x.FlowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.States)
            .HasField("_states")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Transitions)
            .WithOne()
            .HasForeignKey(x => x.FlowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Transitions)
            .HasField("_transitions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.ProjectId);
    }
}
