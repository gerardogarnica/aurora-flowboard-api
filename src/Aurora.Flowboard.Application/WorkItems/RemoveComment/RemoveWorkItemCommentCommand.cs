namespace Aurora.Flowboard.Application.WorkItems.RemoveComment;

public sealed record RemoveWorkItemCommentCommand(
    Guid WorkItemId,
    Guid CommentId) : ICommand;
