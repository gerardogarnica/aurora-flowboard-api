namespace Aurora.Flowboard.Application.WorkItems.GetComments;

internal sealed class GetWorkItemCommentsHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetWorkItemCommentsQuery, PagedResponse<WorkItemCommentResponse>>
{
    public async Task<Result<PagedResponse<WorkItemCommentResponse>>> Handle(
        GetWorkItemCommentsQuery query,
        CancellationToken cancellationToken)
    {
        bool canAccess = await dbContext.CanAccessWorkItemAsync(query.WorkItemId, userContext.UserId, cancellationToken);

        if (!canAccess)
        {
            return Result.Fail<PagedResponse<WorkItemCommentResponse>>(WorkItemErrors.NotFound);
        }

        var commentRows = await dbContext
            .Comments
            .AsNoTracking()
            .Where(c => c.WorkItemId == query.WorkItemId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedOnUtc)
            .ThenByDescending(c => c.Id)
            .ToPagedResponseAsync(
                query,
                c => new
                {
                    c.Id,
                    c.AuthorId,
                    AuthorFirstName = dbContext.Users
                        .Where(u => u.Id == c.AuthorId)
                        .Select(u => u.FirstName)
                        .FirstOrDefault(),
                    AuthorLastName = dbContext.Users
                        .Where(u => u.Id == c.AuthorId)
                        .Select(u => u.LastName)
                        .FirstOrDefault(),
                    c.Content,
                    c.CreatedOnUtc,
                    c.UpdatedOnUtc
                },
                cancellationToken);

        return commentRows.Map(c => new WorkItemCommentResponse(
            c.Id,
            c.AuthorId,
            c.AuthorFirstName is null || c.AuthorLastName is null
                ? string.Empty
                : User.FormatFullName(c.AuthorFirstName, c.AuthorLastName),
            User.FormatInitials(c.AuthorFirstName, c.AuthorLastName),
            c.Content,
            c.CreatedOnUtc,
            c.UpdatedOnUtc));
    }
}
