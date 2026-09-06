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

        IQueryable<Comment> comments = dbContext
            .Comments
            .AsNoTracking()
            .Where(c => c.WorkItemId == query.WorkItemId && !c.IsDeleted);

        int totalCount = await comments.CountAsync(cancellationToken);

        List<WorkItemCommentResponse> items = await comments
            .OrderByDescending(c => c.CreatedOnUtc)
            .ThenByDescending(c => c.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new WorkItemCommentResponse(
                c.Id,
                c.AuthorId,
                dbContext.Users
                    .Where(u => u.Id == c.AuthorId)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault() ?? string.Empty,
                c.Content,
                c.CreatedOnUtc,
                c.UpdatedOnUtc))
            .ToListAsync(cancellationToken);

        return new PagedResponse<WorkItemCommentResponse>(items, query.Page, query.PageSize, totalCount);
    }
}
