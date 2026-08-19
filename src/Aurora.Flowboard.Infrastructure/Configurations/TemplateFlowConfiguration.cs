using Aurora.Flowboard.Domain.TemplateFlows;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class TemplateFlowConfiguration : IEntityTypeConfiguration<TemplateFlow>
{
    public void Configure(EntityTypeBuilder<TemplateFlow> builder)
    {
        builder.ToTable("template_flows");

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
            .HasForeignKey(x => x.TemplateFlowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.States)
            .HasField("_states")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.Kind)
            .IsUnique();
    }
}
