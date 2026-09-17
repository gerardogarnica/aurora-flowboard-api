namespace Aurora.Flowboard.Application.Projects.GetById;

internal sealed class GetProjectByIdHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetProjectByIdQuery, ProjectResponse>
{
    public async Task<Result<ProjectResponse>> Handle(
        GetProjectByIdQuery query,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Members).ThenInclude(m => m.User)
            .Include(p => p.ChangeLogs).ThenInclude(cl => cl.ChangedBy)
            .Include(p => p.Creator)
            .Include(p => p.WorkItems).ThenInclude(wi => wi.FlowState)
            .AsNoTracking()
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (project is null || !project.Members.Any(m => m.UserId == userContext.UserId))
        {
            return Result.Fail<ProjectResponse>(ProjectErrors.NotFound);
        }

        // MemberAdded/MemberRemoved point AffectedEntityId at a User. A removed member is no longer
        // in project.Members, so names are resolved against Users instead of the loaded members.
        List<Guid> affectedUserIds = [.. project.ChangeLogs
            .Where(cl => cl.ChangeType is ProjectChangeType.MemberAdded or ProjectChangeType.MemberRemoved)
            .Where(cl => cl.AffectedEntityId.HasValue)
            .Select(cl => cl.AffectedEntityId!.Value)
            .Distinct()];

        Dictionary<Guid, string> affectedUserNames = affectedUserIds.Count == 0
            ? []
            : await dbContext
                .Users
                .Where(u => affectedUserIds.Contains(u.Id))
                .AsNoTracking()
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        return new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.Prefix.Value,
            project.Color,
            project.Kind,
            project.Status,
            project.WorkItems.Count(wi => wi.FlowState.Category == FlowStateCategory.Active),
            project.WorkItems.Count(wi => wi.FlowState.Category == FlowStateCategory.Completed),
            project.CanModifyFlowStates(),
            project.CanAddOrUpdateWorkItem(),
            project.CreatedBy,
            project.Creator.FullName,
            project.CreatedOnUtc,
            project.UpdatedOnUtc,
            [.. project.Members.OrderBy(m => m.User.FullName).Select(
                m => new ProjectMemberResponse(
                    m.UserId,
                    m.User.FirstName,
                    m.User.LastName,
                    m.User.FullName,
                    m.User.Initials,
                    m.Role,
                    m.JoinedOnUtc))],
            [.. project.ChangeLogs.OrderBy(cl => cl.ChangedOnUtc).Select(
                cl => new ProjectChangeLogResponse(
                    cl.Id,
                    cl.ChangedById,
                    cl.ChangedBy.FullName,
                    cl.ChangedBy.Initials,
                    cl.ChangeType,
                    cl.AffectedEntityId,
                    cl.AffectedEntityId.HasValue
                        ? affectedUserNames.GetValueOrDefault(cl.AffectedEntityId.Value)
                        : null,
                    cl.NewStatus,
                    cl.ChangedOnUtc))]);
    }
}
