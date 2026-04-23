using Aurora.Flowboard.Domain.WorkItems;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.ToTable("work_item_time_entries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WorkItemId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Hours)
            .IsRequired()
            .HasPrecision(8, 2);

        builder.Property(x => x.Description)
            .HasMaxLength(TimeEntry.MaxDescriptionLength);

        builder.Property(x => x.LoggedOnUtc)
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.HasIndex(x => x.WorkItemId);
    }
}
