namespace Aurora.Flowboard.Application.WorkItems.RemoveComment;

internal sealed class RemoveWorkItemCommentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RemoveWorkItemCommentCommand>
{
    public async Task<Result> Handle(
        RemoveWorkItemCommentCommand command,
        CancellationToken cancellationToken)
    {
        WorkItem? workItem = await dbContext
            .WorkItems
            .Include(w => w.Comments)
            .SingleOrDefaultAsync(w => w.Id == command.WorkItemId, cancellationToken);

        if (workItem is null)
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        Result result = workItem.RemoveComment(command.CommentId, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
