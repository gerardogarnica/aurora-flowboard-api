using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProjectId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.JoinedOnUtc)
            .IsRequired();

        builder.HasOne<User>(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ProjectId);

        builder.HasIndex(x => new { x.ProjectId, x.UserId })
            .IsUnique();
    }
}
