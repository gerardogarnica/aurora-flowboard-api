namespace Aurora.Flowboard.Infrastructure.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(p => p.Id).HasColumnName("outbox_id");
        builder.Property(x => x.Type).HasMaxLength(100);
        builder.Property(x => x.Content).HasMaxLength(4000);
        builder.Property(x => x.Error).HasMaxLength(4000);
    }
}
