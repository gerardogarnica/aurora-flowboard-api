using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class ProjectChangeLogConfiguration : IEntityTypeConfiguration<ProjectChangeLog>
{
    public void Configure(EntityTypeBuilder<ProjectChangeLog> builder)
    {
        builder.ToTable("project_change_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProjectId)
            .IsRequired();

        builder.Property(x => x.ChangedById)
            .IsRequired();

        builder.Property(x => x.ChangeType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.AffectedEntityId);

        builder.Property(x => x.ChangedOnUtc)
            .IsRequired();

        builder.HasOne<User>(x => x.ChangedBy)
            .WithMany()
            .HasForeignKey(x => x.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ProjectId);
    }
}
