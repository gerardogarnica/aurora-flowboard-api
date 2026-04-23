using Aurora.Flowboard.Domain.WorkItems;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class WorkItemChangeLogConfiguration : IEntityTypeConfiguration<WorkItemChangeLog>
{
    public void Configure(EntityTypeBuilder<WorkItemChangeLog> builder)
    {
        builder.ToTable("work_item_change_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WorkItemId)
            .IsRequired();

        builder.Property(x => x.ChangedById)
            .IsRequired();

        builder.Property(x => x.ChangeType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.AffectedEntityId);

        builder.Property(x => x.ChangedOnUtc)
            .IsRequired();

        builder.HasIndex(x => x.WorkItemId);
    }
}
