namespace Aurora.Flowboard.Application.WorkItems.UpdateComment;

public sealed record UpdateWorkItemCommentCommand(
    Guid WorkItemId,
    Guid CommentId,
    string Content,
    Guid ChangedById) : ICommand;
