namespace Aurora.Flowboard.Application.WorkItems.AddComment;

internal sealed class AddWorkItemCommentHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddWorkItemCommentCommand>
{
    public async Task<Result> Handle(
        AddWorkItemCommentCommand command,
        CancellationToken cancellationToken)
    {
        WorkItem? workItem = await dbContext
            .WorkItems
            .SingleOrDefaultAsync(w => w.Id == command.WorkItemId, cancellationToken);

        if (workItem is null)
        {
            return Result.Fail(WorkItemErrors.NotFound);
        }

        User? author = await dbContext
            .Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == command.AuthorId, cancellationToken);

        if (author is null)
        {
            return Result.Fail(UserErrors.NotFound);
        }

        Result result = workItem.AddComment(author, command.Content, dateTimeProvider.UtcNow);

        if (!result.IsSuccessful)
        {
            return Result.Fail(result.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
