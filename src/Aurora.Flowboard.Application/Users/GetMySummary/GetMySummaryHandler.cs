namespace Aurora.Flowboard.Application.Users.GetMySummary;

internal sealed class GetMySummaryHandler(
    IApplicationDbContext dbContext) : IQueryHandler<GetMySummaryQuery, MySummaryResponse>
{
    public async Task<Result<MySummaryResponse>> Handle(
        GetMySummaryQuery query,
        CancellationToken cancellationToken)
    {
        User? user = await dbContext
            .Users
            .Include(u => u.Roles)
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Fail<MySummaryResponse>(UserErrors.NotFound);
        }

        List<Project> projects = await dbContext
            .Projects
            .Include(p => p.Members)
            .Include(p => p.WorkItems).ThenInclude(wi => wi.FlowState)
            .Where(p => p.Status != ProjectStatus.Archived && p.Members.Any(m => m.UserId == query.UserId))
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        int memberCount = projects
            .SelectMany(p => p.Members)
            .Select(m => m.UserId)
            .Distinct()
            .Count();

        int myOpenIssues = projects
            .SelectMany(p => p.WorkItems)
            .Count(wi => wi.AssigneeId == query.UserId && wi.FlowState.Category == FlowStateCategory.Active);

        IReadOnlyCollection<MyProjectSummaryResponse> orderedProjects = [.. projects
            .OrderBy(p => p.Status)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(p => new MyProjectSummaryResponse(p.Id, p.Name, p.Color, p.Status))];

        return new MySummaryResponse(
            new MyProfileResponse(user.Id, user.FullName, user.Initials, user.Email.Value, user.Roles.First().Name),
            new MySummaryCountsResponse(projects.Count, memberCount, 0, myOpenIssues),
            orderedProjects);
    }
}
