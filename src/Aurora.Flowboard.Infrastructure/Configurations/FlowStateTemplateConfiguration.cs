using Aurora.Flowboard.Domain.FlowStateTemplates;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class FlowStateTemplateConfiguration : IEntityTypeConfiguration<FlowStateTemplate>
{
    public void Configure(EntityTypeBuilder<FlowStateTemplate> builder)
    {
        builder.ToTable("flow_state_templates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Kind)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.States)
            .WithOne()
            .HasForeignKey(x => x.FlowStateTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.States)
            .HasField("_states")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.Kind)
            .IsUnique();
    }
}
