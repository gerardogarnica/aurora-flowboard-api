namespace Aurora.Flowboard.Application.WorkItems.RemoveComment;

internal sealed class RemoveWorkItemCommentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : WorkItemFieldUpdateHandler<RemoveWorkItemCommentCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(RemoveWorkItemCommentCommand command) => command.WorkItemId;

    protected override IQueryable<WorkItem> Shape(IQueryable<WorkItem> query) =>
        query.Include(w => w.Comments);

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        RemoveWorkItemCommentCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.RemoveComment(command.CommentId, changedBy, utcNow));
}
