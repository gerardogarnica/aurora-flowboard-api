using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class ComponentConfiguration : IEntityTypeConfiguration<Component>
{
    public void Configure(EntityTypeBuilder<Component> builder)
    {
        builder.ToTable("components");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProjectId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Component.MaxNameLength);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc);

        builder.HasIndex(x => x.ProjectId);

        builder.HasIndex(x => new { x.ProjectId, x.Name })
            .IsUnique();
    }
}
