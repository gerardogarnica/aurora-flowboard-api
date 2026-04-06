namespace Aurora.Flowboard.Application.WorkItems.AddComment;

public sealed record AddWorkItemCommentCommand(
    Guid WorkItemId,
    Guid AuthorId,
    string Content) : ICommand;
