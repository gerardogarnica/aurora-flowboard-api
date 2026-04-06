namespace Aurora.Flowboard.Application.Projects.GetById;

internal sealed class GetProjectByIdHandler(
    IApplicationDbContext dbContext) : IQueryHandler<GetProjectByIdQuery, ProjectResponse>
{
    public async Task<Result<ProjectResponse>> Handle(
        GetProjectByIdQuery query,
        CancellationToken cancellationToken)
    {
        Project? project = await dbContext
            .Projects
            .Include(p => p.Members)
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Fail<ProjectResponse>(ProjectErrors.NotFound);
        }

        var response = new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.EstimatedCompletionDate,
            project.Status,
            project.CreatedOnUtc,
            project.UpdatedOnUtc,
            [.. project.Members.Select(m => new ProjectMemberResponse(m.UserId, m.Role, m.JoinedOnUtc))]);

        return response;
    }
}
