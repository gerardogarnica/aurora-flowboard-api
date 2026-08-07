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

        return new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.Prefix.Value,
            project.Color,
            project.Status,
            project.WorkItems.Count(wi => wi.FlowState.Category == FlowStateCategory.Active),
            project.WorkItems.Count(wi => wi.FlowState.Category == FlowStateCategory.Completed),
            project.CanAddOrUpdateFlow(),
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
                    cl.NewStatus,
                    cl.ChangedOnUtc))]);
    }
}
