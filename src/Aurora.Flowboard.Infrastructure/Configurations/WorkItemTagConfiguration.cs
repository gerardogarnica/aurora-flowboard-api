using Aurora.Flowboard.Domain.WorkItems;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class WorkItemTagConfiguration : IEntityTypeConfiguration<WorkItemTag>
{
    public void Configure(EntityTypeBuilder<WorkItemTag> builder)
    {
        builder.ToTable("work_item_tags");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(WorkItemTag.MaxNameLength);

        builder.HasIndex(x => new { x.WorkItemId, x.Name }).IsUnique();
    }
}
