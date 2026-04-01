using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.WorkItems;

public sealed class Comment
{
    public Guid Id { get; private set; }
    public Guid WorkItemId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    private Comment() { } // EF Core

    private Comment(Guid id, Guid workItemId, Guid authorId, string content, DateTime createdOnUtc)
    {
        Id = id;
        WorkItemId = workItemId;
        AuthorId = authorId;
        Content = content;
        IsDeleted = false;
        CreatedOnUtc = createdOnUtc;
    }

    internal static Result<Comment> Create(WorkItem workItem, User author, string content, DateTime createdOnUtc)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Result.Fail<Comment>(WorkItemErrors.CommentContentRequired);
        }

        return new Comment(
            Guid.NewGuid(),
            workItem.Id,
            author.Id,
            content.Trim(),
            createdOnUtc);
    }

    public Result UpdateContent(string content, DateTime updatedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Result.Fail(WorkItemErrors.CommentContentRequired);
        }

        Content = content.Trim();
        UpdatedOnUtc = updatedOnUtc;

        return Result.Ok();
    }

    public Result Remove(DateTime updatedOnUtc)
    {
        IsDeleted = true;
        UpdatedOnUtc = updatedOnUtc;

        return Result.Ok();
    }
}
