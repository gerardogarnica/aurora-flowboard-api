using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.Shared;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Project.MaxNameLength);

        builder.Property(x => x.Description)
            .HasMaxLength(Project.MaxDescriptionLength);

        builder.OwnsOne(x => x.Prefix, prefix =>
        {
            prefix.Property(p => p.Value)
                .HasColumnName("prefix")
                .IsRequired()
                .HasMaxLength(ProjectCode.MaxLength);

            prefix.HasIndex(p => p.Value)
                .IsUnique();
        });

        builder.Property(x => x.Kind)
            .IsRequired()
            .HasConversion<string>();

        builder.OwnsOne(x => x.Color, color =>
        {
            color.Property(c => c.Value)
                .HasColumnName("color")
                .IsRequired()
                .HasMaxLength(Color.MaxLength);
        });

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.WorkItemCounter)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.LastActivityDate)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc);

        builder.HasOne<User>(x => x.Creator)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Members)
            .HasField("_members")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.ChangeLogs)
            .WithOne()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.ChangeLogs)
            .HasField("_changeLogs")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Cascade is load-bearing, not cosmetic: Project.RemoveFlowState severs states and
        // transitions from a required relationship, and EF only delete-orphans under Cascade.
        builder.HasMany(x => x.FlowStates)
            .WithOne()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.FlowStates)
            .HasField("_flowStates")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.FlowTransitions)
            .WithOne()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.FlowTransitions)
            .HasField("_flowTransitions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.WorkItems)
            .HasField("_workItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Components)
            .WithOne(c => c.Project)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Components)
            .HasField("_components")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Milestones)
            .WithOne(m => m.Project)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Milestones)
            .HasField("_milestones")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
