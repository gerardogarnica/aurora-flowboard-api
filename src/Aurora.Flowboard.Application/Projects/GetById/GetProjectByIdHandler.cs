namespace Aurora.Flowboard.Application.Projects.GetById;

internal sealed class GetProjectByIdHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetProjectByIdQuery, ProjectResponse>
{
    public async Task<Result<ProjectResponse>> Handle(
        GetProjectByIdQuery query,
        CancellationToken cancellationToken)
    {
        // Counts are computed in SQL and members are the only collection projected; the change log
        // is served paginated by GET projects/{id}/change-logs.
        var row = await dbContext
            .Projects
            .AsNoTracking()
            .Where(p => p.Id == query.ProjectId && p.Members.Any(m => m.UserId == userContext.UserId))
            .Select(p => new
            {
                Project = p,
                CreatorFirstName = p.Creator.FirstName,
                CreatorLastName = p.Creator.LastName,
                OpenWorkItems = p.WorkItems.Count(wi => wi.FlowState.Category == FlowStateCategory.Active),
                ClosedWorkItems = p.WorkItems.Count(wi => wi.FlowState.Category == FlowStateCategory.Completed),
                Members = p.Members
                    .Select(m => new { m.UserId, m.User.FirstName, m.User.LastName, m.Role, m.JoinedOnUtc })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return Result.Fail<ProjectResponse>(ProjectErrors.NotFound);
        }

        Project project = row.Project;

        return new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.Prefix.Value,
            project.Color,
            project.Kind,
            project.Status,
            row.OpenWorkItems,
            row.ClosedWorkItems,
            project.CanModifyFlowStates(),
            project.CanAddOrUpdateWorkItem(),
            project.CreatedBy,
            User.FormatFullName(row.CreatorFirstName, row.CreatorLastName),
            project.CreatedOnUtc,
            project.UpdatedOnUtc,
            [.. row.Members
                .Select(m => new ProjectMemberResponse(
                    m.UserId,
                    m.FirstName,
                    m.LastName,
                    User.FormatFullName(m.FirstName, m.LastName),
                    User.FormatInitials(m.FirstName, m.LastName),
                    m.Role,
                    m.JoinedOnUtc))
                .OrderBy(m => m.FullName)]);
    }
}
