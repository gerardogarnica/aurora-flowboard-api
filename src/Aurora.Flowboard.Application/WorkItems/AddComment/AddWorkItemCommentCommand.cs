namespace Aurora.Flowboard.Application.WorkItems.AddComment;

public sealed record AddWorkItemCommentCommand(
    Guid WorkItemId,
    string Content) : ICommand;
