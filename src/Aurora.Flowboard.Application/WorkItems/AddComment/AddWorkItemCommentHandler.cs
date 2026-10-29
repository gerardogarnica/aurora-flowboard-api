namespace Aurora.Flowboard.Application.WorkItems.AddComment;

internal sealed class AddWorkItemCommentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : WorkItemFieldUpdateHandler<AddWorkItemCommentCommand>(dbContext, dateTimeProvider, userContext)
{
    protected override Guid GetWorkItemId(AddWorkItemCommentCommand command) => command.WorkItemId;

    protected override Task<Result> ApplyAsync(
        WorkItem workItem,
        AddWorkItemCommentCommand command,
        User changedBy,
        DateTime utcNow,
        CancellationToken cancellationToken) =>
        Task.FromResult(workItem.AddComment(changedBy, command.Content, utcNow));
}
