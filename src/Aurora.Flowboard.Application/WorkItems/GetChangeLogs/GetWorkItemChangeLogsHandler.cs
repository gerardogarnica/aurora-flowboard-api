namespace Aurora.Flowboard.Application.WorkItems.GetChangeLogs;

internal sealed class GetWorkItemChangeLogsHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetWorkItemChangeLogsQuery, PagedResponse<WorkItemChangeLogResponse>>
{
    public async Task<Result<PagedResponse<WorkItemChangeLogResponse>>> Handle(
        GetWorkItemChangeLogsQuery query,
        CancellationToken cancellationToken)
    {
        bool canAccess = await dbContext.CanAccessWorkItemAsync(query.WorkItemId, userContext.UserId, cancellationToken);

        if (!canAccess)
        {
            return Result.Fail<PagedResponse<WorkItemChangeLogResponse>>(WorkItemErrors.NotFound);
        }

        IQueryable<WorkItemChangeLog> changeLogs = dbContext
            .WorkItemChangeLogs
            .AsNoTracking()
            .Where(c => c.WorkItemId == query.WorkItemId);

        int totalCount = await changeLogs.CountAsync(cancellationToken);

        List<WorkItemChangeLogResponse> items = await changeLogs
            .OrderByDescending(c => c.ChangedOnUtc)
            .ThenByDescending(c => c.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new WorkItemChangeLogResponse(
                c.Id,
                c.ChangedById,
                dbContext.Users
                    .Where(u => u.Id == c.ChangedById)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault() ?? string.Empty,
                c.ChangeType,
                c.AffectedEntityId,
                c.ChangeType == WorkItemChangeType.Assigned
                    ? dbContext.Users
                        .Where(u => u.Id == c.AffectedEntityId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault()
                    : c.ChangeType == WorkItemChangeType.Moved
                        ? dbContext.FlowStates
                            .Where(fs => fs.Id == c.AffectedEntityId)
                            .Select(fs => fs.Name)
                            .FirstOrDefault()
                        : c.ChangeType == WorkItemChangeType.ComponentChanged
                            ? dbContext.Components
                                .Where(comp => comp.Id == c.AffectedEntityId)
                                .Select(comp => comp.Name)
                                .FirstOrDefault()
                            : c.ChangeType == WorkItemChangeType.MilestoneChanged
                                ? dbContext.Milestones
                                    .Where(m => m.Id == c.AffectedEntityId)
                                    .Select(m => m.Name)
                                    .FirstOrDefault()
                                : null,
                c.ChangedOnUtc))
            .ToListAsync(cancellationToken);

        return new PagedResponse<WorkItemChangeLogResponse>(items, query.Page, query.PageSize, totalCount);
    }
}
