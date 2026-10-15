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

        // Three scalar queries over the same filter: nothing loads members or work items into memory.
        IQueryable<Project> myProjects = dbContext
            .Projects
            .AsNoTracking()
            .Where(p => p.Status != ProjectStatus.Archived && p.Members.Any(m => m.UserId == query.UserId));

        List<MyProjectSummaryResponse> projects = await myProjects
            .Select(p => new MyProjectSummaryResponse(p.Id, p.Name, p.Color.Value, p.Status))
            .ToListAsync(cancellationToken);

        int memberCount = await myProjects
            .SelectMany(p => p.Members)
            .Select(m => m.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        int myOpenIssues = await myProjects
            .SelectMany(p => p.WorkItems)
            .CountAsync(
                wi => wi.AssigneeId == query.UserId && wi.FlowState.Category == FlowStateCategory.Active,
                cancellationToken);

        IReadOnlyCollection<MyProjectSummaryResponse> orderedProjects = [.. projects
            .OrderBy(p => p.Status)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)];

        return new MySummaryResponse(
            new MyProfileResponse(user.Id, user.FullName, user.Initials, user.Email.Value, user.Roles.First().Name),
            new MySummaryCountsResponse(projects.Count, memberCount, 0, myOpenIssues),
            orderedProjects);
    }
}
