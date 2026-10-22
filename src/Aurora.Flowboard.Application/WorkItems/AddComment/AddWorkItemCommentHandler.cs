namespace Aurora.Flowboard.Application.WorkItems.AddComment;

internal sealed class AddWorkItemCommentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext) : ICommandHandler<AddWorkItemCommentCommand>
{
    public async Task<Result> Handle(
        AddWorkItemCommentCommand command,
        CancellationToken cancellationToken)
    {
        WorkItem? workItem = await dbContext
            .WorkItems
            .Include(w => w.Project)
            .ThenInclude(p => p.Members)
            .AsSplitQuery()
            .SingleOrDefaultAsync(w => w.Id == command.WorkItemId, cancellationToken);

        if (workItem is null)
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        Result<User> authorResult = await dbContext.GetCurrentUserAsync(userContext, cancellationToken);

        if (!authorResult.IsSuccessful)
        {
            return Result.Fail(authorResult.Error);
        }

        Result result = workItem.AddComment(authorResult.Value, command.Content, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
