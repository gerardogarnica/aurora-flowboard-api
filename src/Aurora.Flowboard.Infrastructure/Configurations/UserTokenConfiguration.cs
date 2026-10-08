using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    // Postgres' xmin system column, used as an optimistic concurrency token: two requests that
    // redeem the same refresh token cannot both update the row. No migration column is generated.
    private const string VersionPropertyName = "Version";
    private const string VersionColumnName = "xmin";

    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("user_tokens");

        builder.HasKey(x => x.UserTokenId);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.AccessTokenId)
            .IsRequired()
            .HasMaxLength(UserToken.MaxAccessTokenIdLength);

        builder.Property(x => x.RefreshTokenHash)
            .IsRequired()
            .HasMaxLength(UserToken.RefreshTokenHashLength)
            .IsFixedLength();

        builder.Property(x => x.AccessTokenExpiresOnUtc)
            .IsRequired();

        builder.Property(x => x.RefreshTokenExpiresOnUtc)
            .IsRequired();

        builder.Property(x => x.IssuedOnUtc)
            .IsRequired();

        builder.Property(x => x.IsRevoked)
            .IsRequired();

        // Explicit column name so EFCore.NamingConventions does not rename it to "version".
        builder.Property<uint>(VersionPropertyName)
            .IsRowVersion()
            .HasColumnName(VersionColumnName);

        builder.HasOne<User>(x => x.User)
            .WithMany(u => u.Tokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.RefreshTokenHash)
            .IsUnique();

        // Supports the UserTokenCleanupJob range delete.
        builder.HasIndex(x => x.RefreshTokenExpiresOnUtc);
    }
}
