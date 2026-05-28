using Aurora.Flowboard.Domain.Shared;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(User.MaxNameLength);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(User.MaxNameLength);

        builder.Ignore(x => x.FullName);

        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("email")
                .IsRequired()
                .HasMaxLength(Email.MaxLength);

            email.HasIndex(e => e.Value)
                .IsUnique();
        });

        builder.OwnsOne<Password>("Password", password =>
        {
            password.Property(p => p.Hash)
                .HasColumnName("password_hash")
                .IsRequired()
                .HasMaxLength(Password.MaxHashLength);
        });

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc);

        builder.OwnsMany(x => x.Roles, role =>
        {
            role.ToTable("user_roles");

            role.WithOwner().HasForeignKey("UserId");

            role.HasKey("UserId", nameof(Role.Name));

            role.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(50);
        });

        builder.Navigation(x => x.Roles)
            .HasField("_roles")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasMany(x => x.Tokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Tokens)
            .HasField("_tokens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
