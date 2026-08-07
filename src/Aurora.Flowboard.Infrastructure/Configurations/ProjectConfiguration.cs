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

        builder.OwnsOne(x => x.Color, color =>
        {
            color.Property(c => c.Value)
                .HasColumnName("color")
                .IsRequired()
                .HasMaxLength(Color.MaxLength);
        });

        builder.Property(x => x.Kind)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

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

        builder.Navigation(x => x.Flows)
            .HasField("_flows")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.WorkItems)
            .HasField("_workItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
