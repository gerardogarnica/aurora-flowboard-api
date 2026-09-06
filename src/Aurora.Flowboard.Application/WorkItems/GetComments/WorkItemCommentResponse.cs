namespace Aurora.Flowboard.Application.WorkItems.GetComments;

public sealed record WorkItemCommentResponse(
    Guid CommentId,
    Guid AuthorId,
    string AuthorFullName,
    string Content,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
