using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("user_tokens");

        builder.HasKey(x => x.UserTokenId);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.AccessToken)
            .IsRequired()
            .HasMaxLength(UserToken.MaxAccessTokenLength);

        builder.Property(x => x.RefreshToken)
            .IsRequired()
            .HasMaxLength(UserToken.MaxRefreshTokenLength);

        builder.Property(x => x.AccessTokenExpiresOnUtc)
            .IsRequired();

        builder.Property(x => x.RefreshTokenExpiresOnUtc)
            .IsRequired();

        builder.Property(x => x.IssuedOnUtc)
            .IsRequired();

        builder.Property(x => x.IsRevoked)
            .IsRequired();

        builder.HasOne<User>(x => x.User)
            .WithMany(u => u.Tokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.RefreshToken)
            .IsUnique();
    }
}
