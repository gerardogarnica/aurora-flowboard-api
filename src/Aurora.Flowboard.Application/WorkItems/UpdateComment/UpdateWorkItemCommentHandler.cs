namespace Aurora.Flowboard.Application.WorkItems.UpdateComment;

internal sealed class UpdateWorkItemCommentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : WorkItemFieldUpdateHandler<UpdateWorkItemCommentCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(UpdateWorkItemCommentCommand command) => command.WorkItemId;

    protected override IQueryable<WorkItem> Shape(IQueryable<WorkItem> query) =>
        query.Include(w => w.Comments);

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        UpdateWorkItemCommentCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.UpdateComment(command.CommentId, changedBy, command.Content, utcNow));
}
