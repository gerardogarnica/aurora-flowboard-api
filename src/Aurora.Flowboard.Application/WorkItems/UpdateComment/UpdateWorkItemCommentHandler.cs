namespace Aurora.Flowboard.Application.WorkItems.UpdateComment;

internal sealed class UpdateWorkItemCommentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateWorkItemCommentCommand>
{
    public async Task<Result> Handle(
        UpdateWorkItemCommentCommand command,
        CancellationToken cancellationToken)
    {
        WorkItem? workItem = await dbContext
            .WorkItems
            .Include(w => w.Comments)
            .Include(w => w.Project)
            .ThenInclude(p => p.Members)
            .AsSplitQuery()
            .SingleOrDefaultAsync(w => w.Id == command.WorkItemId, cancellationToken);

        if (workItem is null)
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        User? changedBy = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.ChangedById, cancellationToken);

        if (changedBy is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = workItem.UpdateComment(command.CommentId, changedBy, command.Content, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
