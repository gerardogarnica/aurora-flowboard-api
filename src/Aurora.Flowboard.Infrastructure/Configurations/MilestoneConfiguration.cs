using Aurora.Flowboard.Domain.Milestones;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> builder)
    {
        builder.ToTable("milestones");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProjectId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Milestone.MaxNameLength);

        builder.Property(x => x.Description)
            .HasMaxLength(Milestone.MaxDescriptionLength);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.TargetStartDate);

        builder.Property(x => x.TargetEndDate);

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ProjectId);

        builder.HasIndex(x => new { x.ProjectId, x.Name })
            .IsUnique();
    }
}
