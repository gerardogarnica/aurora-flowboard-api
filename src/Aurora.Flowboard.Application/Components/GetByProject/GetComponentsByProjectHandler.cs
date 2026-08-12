namespace Aurora.Flowboard.Application.Components.GetByProject;

internal sealed class GetComponentsByProjectHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IQueryHandler<GetComponentsByProjectQuery, IReadOnlyCollection<ComponentResponse>>
{
    public async Task<Result<IReadOnlyCollection<ComponentResponse>>> Handle(
        GetComponentsByProjectQuery query,
        CancellationToken cancellationToken)
    {
        bool isMember = await dbContext
            .Projects
            .AsNoTracking()
            .AnyAsync(p => p.Id == query.ProjectId && p.Members.Any(m => m.UserId == userContext.UserId), cancellationToken);

        if (!isMember)
        {
            return Result.Fail<IReadOnlyCollection<ComponentResponse>>(ProjectErrors.NotFound);
        }

        List<ComponentResponse> components = await dbContext
            .Components
            .AsNoTracking()
            .Where(c => c.ProjectId == query.ProjectId)
            .OrderBy(c => c.Name)
            .Select(c => new ComponentResponse(
                c.Id,
                c.Name,
                c.Status,
                c.CreatedBy,
                c.CreatedOnUtc,
                c.UpdatedOnUtc))
            .ToListAsync(cancellationToken);

        return components;
    }
}
