using Aurora.Flowboard.Domain.Flows;
using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.WorkItems;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("work_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(WorkItem.MaxTitleLength);

        builder.Property(x => x.Description)
            .HasMaxLength(WorkItem.MaxDescriptionLength);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Priority)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(WorkItem.MaxCodeLength);

        builder.Property(x => x.SequenceNumber)
            .IsRequired();

        builder.Property(x => x.EstimatedPoints);

        builder.Property(x => x.EstimatedCompletionDate);

        builder.Property(x => x.CreatedById)
            .IsRequired();

        builder.Property(x => x.AssigneeId);

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc);

        builder.Property(x => x.CompletedOnUtc);

        builder.HasOne<Project>(x => x.Project)
            .WithMany(p => p.WorkItems)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FlowState>(x => x.FlowState)
            .WithMany()
            .HasForeignKey(x => x.FlowStateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Component)
            .WithMany()
            .HasForeignKey(x => x.ComponentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Milestone)
            .WithMany()
            .HasForeignKey(x => x.MilestoneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Tags)
            .WithOne()
            .HasForeignKey(x => x.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Tags)
            .HasField("_tags")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.Comments)
            .WithOne()
            .HasForeignKey(x => x.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Comments)
            .HasField("_comments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.TimeEntries)
            .WithOne()
            .HasForeignKey(x => x.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.TimeEntries)
            .HasField("_timeEntries")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.StateHistory)
            .WithOne()
            .HasForeignKey(x => x.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.StateHistory)
            .HasField("_stateHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.ChangeLogs)
            .WithOne()
            .HasForeignKey(x => x.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.ChangeLogs)
            .HasField("_changeLogs")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasIndex(x => x.ProjectId);

        builder.HasIndex(x => x.ComponentId);

        builder.HasIndex(x => x.MilestoneId);
    }
}
