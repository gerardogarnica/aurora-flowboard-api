using Aurora.Flowboard.Domain.WorkItems;

namespace Aurora.Flowboard.Infrastructure.Configurations;

internal sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("work_item_comments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.WorkItemId)
            .IsRequired();

        builder.Property(x => x.AuthorId)
            .IsRequired();

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(Comment.MaxContentLength);

        builder.Property(x => x.IsDeleted)
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedOnUtc);

        builder.HasIndex(x => x.WorkItemId);
    }
}
