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
            project.Code,
            project.Color,
            project.EstimatedCompletionDate,
            project.Status,
            project.CreatedBy,
            project.Creator.FullName,
            project.CreatedOnUtc,
            project.UpdatedOnUtc,
            [.. project.Members.OrderBy(m => m.User.FullName).Select(m => new ProjectMemberResponse(m.UserId, m.User.FullName, m.Role, m.JoinedOnUtc))],
            [.. project.ChangeLogs.OrderBy(cl => cl.ChangedOnUtc).Select(cl => new ProjectChangeLogResponse(cl.Id, cl.ChangedById, cl.ChangedBy.FullName, cl.ChangeType, cl.AffectedEntityId, cl.NewStatus, cl.ChangedOnUtc))]);
    }
}
