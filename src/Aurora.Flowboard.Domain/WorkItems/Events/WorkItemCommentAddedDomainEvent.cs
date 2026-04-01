namespace Aurora.Flowboard.Domain.WorkItems.Events;

public sealed class WorkItemCommentAddedDomainEvent(Guid workItemId, Guid commentId) : DomainEvent
{
    public Guid WorkItemId { get; init; } = workItemId;
    public Guid CommentId { get; init; } = commentId;
}
