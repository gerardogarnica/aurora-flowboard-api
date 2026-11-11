namespace Aurora.Flowboard.Application.Dashboard.GetDashboard;

// Reads the rows DashboardCalculator turns into the response. Every open-work counter comes from one
// GROUP BY, so they always add up. The milestone closed counts, closedLast7Days and oldestInFlight are
// separate reads: a concurrent move can make them disagree with it for one load (spec, section 6).
internal sealed class GetDashboardHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : IQueryHandler<GetDashboardQuery, DashboardResponse>
{
    public async Task<Result<DashboardResponse>> Handle(
        GetDashboardQuery query,
        CancellationToken cancellationToken)
    {
        Guid userId = userContext.UserId;
        DateTime now = dateTimeProvider.UtcNow;

        // Every read covers my active projects only (MyProjectsQueryExtensions).
        List<DashboardProjectRow> projects = await dbContext
            .Projects
            .AsNoTracking()
            .WhereMyActiveProjects(userId)
            .Select(p => new DashboardProjectRow(p.Id, p.Name, p.Color.Value, p.Kind, p.Status))
            .ToListAsync(cancellationToken);

        if (projects.Count == 0)
        {
            return DashboardCalculator.Build(DashboardSnapshot.Empty);
        }

        List<DashboardStateRow> states = await dbContext
            .Projects
            .AsNoTracking()
            .WhereMyActiveProjects(userId)
            .SelectMany(p => p.FlowStates)
            .Where(s => s.Category == FlowStateCategory.Active)
            .Select(s => new DashboardStateRow(s.Id, s.ProjectId, s.Name, s.Color.Value, s.SortOrder))
            .ToListAsync(cancellationToken);

        HashSet<Guid> backlogStateIds = FlowStateStageResolver.GetBacklogStateIds(
            states.Select(s => (s.Id, s.ProjectId, s.SortOrder)));
        List<Guid> openStateIds = [.. states.Select(s => s.Id)];
        List<Guid> inFlightStateIds = [.. openStateIds.Where(id => !backlogStateIds.Contains(id))];

        List<DashboardOpenGroupRow> openGroups = await GetOpenGroupsAsync(openStateIds, userId, now, cancellationToken);
        List<DashboardMilestoneRow> milestones = await GetActiveMilestonesAsync(userId, cancellationToken);
        Dictionary<Guid, int> closedByMilestone = await GetClosedByMilestoneAsync([.. milestones.Select(m => m.Id)], cancellationToken);
        int closedLast7Days = await CountClosedLast7DaysAsync([.. projects.Select(p => p.Id)], now, cancellationToken);
        DashboardOldestInFlightResponse? oldestInFlight = await GetOldestInFlightAsync(inFlightStateIds, cancellationToken);
        List<DashboardMemberRow> members = await GetMembersAsync(userId, cancellationToken);

        return DashboardCalculator.Build(new DashboardSnapshot(
            projects,
            states,
            openGroups,
            milestones,
            closedByMilestone,
            closedLast7Days,
            oldestInFlight,
            members));
    }

    private async Task<List<DashboardOpenGroupRow>> GetOpenGroupsAsync(
        List<Guid> openStateIds,
        Guid userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (openStateIds.Count == 0)
        {
            return [];
        }

        // Whole 24-hour blocks: entered at or before now - N days is N days or more in the state.
        // A ternary chain, because expression trees cannot hold a switch (CS8514).
        DateTime agingSince = now.AddDays(-DashboardCalculator.AgingThresholdDays);
        DateTime stalledSince = now.AddDays(-WorkItem.StalledThresholdDays);
        DateTime oldSince = now.AddDays(-DashboardCalculator.OldThresholdDays);

        return await dbContext
            .WorkItems
            .AsNoTracking()
            .Where(w => openStateIds.Contains(w.FlowStateId))
            .GroupBy(w => new
            {
                w.FlowStateId,
                w.MilestoneId,
                IsMine = w.AssigneeId == userId,
                IsUnassigned = w.AssigneeId == null,
                IsCriticalHigh = w.Priority == Priority.High || w.Priority == Priority.Critical,
                AgeBand = w.EnteredCurrentStateOnUtc > agingSince
                    ? DashboardCalculator.FreshBand
                    : w.EnteredCurrentStateOnUtc > stalledSince
                        ? DashboardCalculator.AgingBand
                        : w.EnteredCurrentStateOnUtc > oldSince
                            ? DashboardCalculator.StalledBand
                            : DashboardCalculator.OldBand
            })
            .Select(g => new DashboardOpenGroupRow(
                g.Key.FlowStateId,
                g.Key.MilestoneId,
                g.Key.IsMine,
                g.Key.IsUnassigned,
                g.Key.IsCriticalHigh,
                g.Key.AgeBand,
                g.Count()))
            .ToListAsync(cancellationToken);
    }

    private Task<List<DashboardMilestoneRow>> GetActiveMilestonesAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext
            .Projects
            .AsNoTracking()
            .WhereMyActiveProjects(userId)
            .SelectMany(p => p.Milestones)
            .Where(m => m.Status == MilestoneStatus.Active)
            .Select(m => new DashboardMilestoneRow(m.Id, m.ProjectId, m.Name, m.Color.Value, m.TargetEndDate, m.CreatedOnUtc))
            .ToListAsync(cancellationToken);

    // Completed with any date. A Cancelled item no longer counts towards its milestone.
    private async Task<Dictionary<Guid, int>> GetClosedByMilestoneAsync(List<Guid> milestoneIds, CancellationToken cancellationToken)
    {
        if (milestoneIds.Count == 0)
        {
            return [];
        }

        List<DashboardMilestoneCountRow> rows = await dbContext
            .WorkItems
            .AsNoTracking()
            .Where(w => w.MilestoneId != null
                && milestoneIds.Contains(w.MilestoneId.Value)
                && w.FlowState.Category == FlowStateCategory.Completed)
            .GroupBy(w => w.MilestoneId ?? Guid.Empty)
            .Select(g => new DashboardMilestoneCountRow(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.MilestoneId, r => r.Count);
    }

    // By category and not by CompletedOnUtc alone: Move also sets it when cancelling, and nothing clears
    // it when an item is reopened.
    private Task<int> CountClosedLast7DaysAsync(List<Guid> projectIds, DateTime now, CancellationToken cancellationToken)
    {
        DateTime closedSince = now.AddDays(-DashboardCalculator.ClosedWindowDays);

        return dbContext
            .WorkItems
            .AsNoTracking()
            .CountAsync(
                w => projectIds.Contains(w.ProjectId)
                    && w.FlowState.Category == FlowStateCategory.Completed
                    && w.CompletedOnUtc >= closedSince,
                cancellationToken);
    }

    private async Task<DashboardOldestInFlightResponse?> GetOldestInFlightAsync(List<Guid> inFlightStateIds, CancellationToken cancellationToken)
    {
        if (inFlightStateIds.Count == 0)
        {
            return null;
        }

        return await dbContext
            .WorkItems
            .AsNoTracking()
            .Where(w => inFlightStateIds.Contains(w.FlowStateId))
            .OrderBy(w => w.EnteredCurrentStateOnUtc)
            .ThenBy(w => w.Id)
            .Select(w => new DashboardOldestInFlightResponse(w.Code, w.Title, w.ProjectId, w.Project.Name, w.EnteredCurrentStateOnUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Names through correlated subqueries: projecting the User would also load its roles (ef-core-queries.md).
    private Task<List<DashboardMemberRow>> GetMembersAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext
            .Projects
            .AsNoTracking()
            .WhereMyActiveProjects(userId)
            .SelectMany(p => p.Members)
            .Select(m => new DashboardMemberRow(
                m.ProjectId,
                m.UserId,
                dbContext.Users.Where(u => u.Id == m.UserId).Select(u => u.FirstName).FirstOrDefault(),
                dbContext.Users.Where(u => u.Id == m.UserId).Select(u => u.LastName).FirstOrDefault()))
            .ToListAsync(cancellationToken);
}
