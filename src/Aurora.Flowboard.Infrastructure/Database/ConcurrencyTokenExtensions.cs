namespace Aurora.Flowboard.Infrastructure.Database;

internal static class ConcurrencyTokenExtensions
{
    // Postgres' xmin system column as an optimistic concurrency token: an UPDATE or DELETE of a row that
    // changed after it was read affects 0 rows, and EF throws DbUpdateConcurrencyException.
    // Npgsql emits no SQL for xmin, so the migration only changes the model snapshot.
    private const string VersionPropertyName = "Version";
    private const string VersionColumnName = "xmin";

    internal static EntityTypeBuilder<TEntity> HasXminConcurrencyToken<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        // Explicit column name so EFCore.NamingConventions does not rename it to "version".
        builder.Property<uint>(VersionPropertyName)
            .IsRowVersion()
            .HasColumnName(VersionColumnName);

        return builder;
    }
}
