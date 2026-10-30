namespace Aurora.Flowboard.Application.Projects.GetChangeLogs;

internal sealed class GetProjectChangeLogsHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetProjectChangeLogsQuery, PagedResponse<ProjectChangeLogResponse>>
{
    public async Task<Result<PagedResponse<ProjectChangeLogResponse>>> Handle(
        GetProjectChangeLogsQuery query,
        CancellationToken cancellationToken)
    {
        bool isMember = await dbContext.IsProjectMemberAsync(query.ProjectId, userContext.UserId, cancellationToken);

        if (!isMember)
        {
            return Result.Fail<PagedResponse<ProjectChangeLogResponse>>(ProjectErrors.NotFound);
        }

        // MemberAdded/MemberRemoved point AffectedEntityId at a User. A removed member is no longer
        // in the project, so the name is resolved against Users rather than the members.
        var rows = await dbContext
            .ProjectChangeLogs
            .AsNoTracking()
            .Where(cl => cl.ProjectId == query.ProjectId)
            .OrderByDescending(cl => cl.ChangedOnUtc)
            .ThenByDescending(cl => cl.Id)
            .ToPagedResponseAsync(
                query,
                cl => new
                {
                    cl.Id,
                    cl.ChangedById,
                    ChangedByFirstName = cl.ChangedBy.FirstName,
                    ChangedByLastName = cl.ChangedBy.LastName,
                    cl.ChangeType,
                    cl.AffectedEntityId,
                    AffectedEntityName =
                        cl.ChangeType == ProjectChangeType.MemberAdded || cl.ChangeType == ProjectChangeType.MemberRemoved
                            ? dbContext.Users
                                .Where(u => u.Id == cl.AffectedEntityId)
                                .Select(u => u.FirstName + " " + u.LastName)
                                .FirstOrDefault()
                            : null,
                    cl.NewStatus,
                    cl.ChangedOnUtc
                },
                cancellationToken);

        return rows.Map(cl => new ProjectChangeLogResponse(
            cl.Id,
            cl.ChangedById,
            User.FormatFullName(cl.ChangedByFirstName, cl.ChangedByLastName),
            User.FormatInitials(cl.ChangedByFirstName, cl.ChangedByLastName),
            cl.ChangeType,
            cl.AffectedEntityId,
            cl.AffectedEntityName,
            cl.NewStatus,
            cl.ChangedOnUtc));
    }
}
