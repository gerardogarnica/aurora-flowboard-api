namespace Aurora.Flowboard.Application.Dashboard.GetDashboard;

// Builds the dashboard from the rows GetDashboardHandler reads. A pure function: every rule of the
// spec's definitions that is not a SQL filter lives here, so it is tested without a database.
internal static class DashboardCalculator
{
    // Age band of an open item, by whole 24-hour blocks in its state. GetDashboardHandler computes it
    // in SQL. StalledBand and OldBand are stalled when the item is in flight.
    internal const int FreshBand = 0;
    internal const int AgingBand = 1;
    internal const int StalledBand = 2;
    internal const int OldBand = 3;

    internal const int AgingThresholdDays = 4;
    internal const int OldThresholdDays = 14;
    internal const int ClosedWindowDays = 7;

    // 0-3, 4-6, 7-13 and 14+ days. The 4 is only a bucket limit: it is not exposed to the frontend.
    private static readonly (int Band, int From, int? To)[] Buckets =
    [
        (FreshBand, 0, AgingThresholdDays - 1),
        (AgingBand, AgingThresholdDays, WorkItem.StalledThresholdDays - 1),
        (StalledBand, WorkItem.StalledThresholdDays, OldThresholdDays - 1),
        (OldBand, OldThresholdDays, null)
    ];

    internal static DashboardResponse Build(DashboardSnapshot snapshot)
    {
        // The handler already reads only active projects; filtering again keeps a Completed project out
        // of every number and of the portfolio, whatever rows it is given.
        List<DashboardProjectRow> activeProjects = [.. snapshot.Projects
            .Where(p => MyProjectsQueryExtensions.AcceptsOpenWork(p.Status))];
        HashSet<Guid> activeProjectIds = [.. activeProjects.Select(p => p.Id)];
        HashSet<Guid> activeStateIds = [.. snapshot.States
            .Where(s => activeProjectIds.Contains(s.ProjectId))
            .Select(s => s.Id)];
        HashSet<Guid> backlogStateIds = FlowStateStageResolver.GetBacklogStateIds(
            snapshot.States.Select(s => (s.Id, s.ProjectId, s.SortOrder)));

        List<DashboardOpenGroupRow> open = [.. snapshot.OpenGroups.Where(g => activeStateIds.Contains(g.FlowStateId))];
        List<DashboardOpenGroupRow> inFlight = [.. open.Where(g => !backlogStateIds.Contains(g.FlowStateId))];
        List<DashboardOpenGroupRow> stalled = [.. inFlight.Where(g => g.AgeBand >= StalledBand)];
        List<DashboardMilestoneRow> milestoneRows = [.. snapshot.Milestones.Where(m => activeProjectIds.Contains(m.ProjectId))];

        List<DashboardMilestoneResponse> milestones = BuildMilestones(snapshot, milestoneRows, open, stalled);

        return new DashboardResponse(
            new DashboardMeResponse(
                Count(open.Where(g => g.IsMine)),
                Count(stalled.Where(g => g.IsMine)),
                Count(inFlight.Where(g => g.IsMine)),
                Count(open.Where(g => g.IsMine && g.IsCriticalHigh))),
            new DashboardTeamResponse(
                Count(stalled),
                Count(open.Where(g => g.IsUnassigned)),
                snapshot.ClosedLast7Days),
            new DashboardAgingResponse(
                [.. Buckets.Select(b => new DashboardAgingBucketResponse(b.From, b.To, Count(inFlight.Where(g => g.AgeBand == b.Band))))],
                snapshot.OldestInFlight),
            milestones,
            BuildPortfolio(snapshot, activeProjects, milestoneRows, milestones.ToDictionary(m => m.Id), open, stalled));
    }

    private static List<DashboardMilestoneResponse> BuildMilestones(
        DashboardSnapshot snapshot,
        List<DashboardMilestoneRow> milestoneRows,
        List<DashboardOpenGroupRow> open,
        List<DashboardOpenGroupRow> stalled)
    {
        Dictionary<Guid, string> projectNames = snapshot.Projects.ToDictionary(p => p.Id, p => p.Name);

        return [.. milestoneRows
            .Select(m =>
            {
                int closed = snapshot.ClosedByMilestone.GetValueOrDefault(m.Id);

                return new DashboardMilestoneResponse(
                    m.Id,
                    m.ProjectId,
                    projectNames[m.ProjectId],
                    m.Name,
                    m.Color,
                    m.TargetEndDate,
                    Count(open.Where(g => g.MilestoneId == m.Id)) + closed,
                    closed,
                    Count(stalled.Where(g => g.MilestoneId == m.Id)));
            })
            // Dated first, the soonest (most overdue) first; then undated, the most stalled first.
            .OrderBy(m => m.TargetEndDate is null)
            .ThenBy(m => m.TargetEndDate)
            .ThenByDescending(m => m.TargetEndDate is null ? m.LinkedStalled : 0)
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Id)];
    }

    private static List<DashboardProjectResponse> BuildPortfolio(
        DashboardSnapshot snapshot,
        List<DashboardProjectRow> activeProjects,
        List<DashboardMilestoneRow> milestoneRows,
        Dictionary<Guid, DashboardMilestoneResponse> milestonesById,
        List<DashboardOpenGroupRow> open,
        List<DashboardOpenGroupRow> stalled) =>
        [.. activeProjects
            .OrderLikeSidebar(p => p.Status, p => p.Name)
            .Select(p =>
            {
                List<DashboardStateRow> states = [.. snapshot.States.Where(s => s.ProjectId == p.Id).OrderBy(s => s.SortOrder)];
                HashSet<Guid> stateIds = [.. states.Select(s => s.Id)];

                return new DashboardProjectResponse(
                    p.Id,
                    p.Name,
                    p.Color,
                    p.Kind,
                    p.Status,
                    Count(open.Where(g => stateIds.Contains(g.FlowStateId))),
                    Count(stalled.Where(g => stateIds.Contains(g.FlowStateId))),
                    [.. states.Select(s => new DashboardFlowStateCountResponse(s.Id, s.Name, s.Color, Count(open.Where(g => g.FlowStateId == s.Id))))],
                    PickActiveMilestone([.. milestoneRows.Where(m => m.ProjectId == p.Id)], milestonesById),
                    [.. snapshot.Members
                        .Where(m => m.ProjectId == p.Id)
                        .Select(ToMember)
                        .OrderBy(m => m.FullName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(m => m.UserId)]);
            })];

    // The Active milestone with the earliest target end date, overdue ones included. When none has a
    // date, the most recently created one.
    private static DashboardActiveMilestoneResponse? PickActiveMilestone(
        List<DashboardMilestoneRow> projectMilestones,
        Dictionary<Guid, DashboardMilestoneResponse> milestonesById)
    {
        DashboardMilestoneRow? picked = projectMilestones
            .Where(m => m.TargetEndDate is not null)
            .OrderBy(m => m.TargetEndDate)
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Id)
            .FirstOrDefault()
            ?? projectMilestones
                .OrderByDescending(m => m.CreatedOnUtc)
                .ThenBy(m => m.Id)
                .FirstOrDefault();

        if (picked is null)
        {
            return null;
        }

        DashboardMilestoneResponse counts = milestonesById[picked.Id];

        return new DashboardActiveMilestoneResponse(
            picked.Id,
            picked.Name,
            picked.Color,
            picked.TargetEndDate,
            counts.LinkedTotal,
            counts.LinkedClosed);
    }

    private static DashboardMemberResponse ToMember(DashboardMemberRow member) =>
        new(
            member.UserId,
            member.FirstName is null || member.LastName is null
                ? string.Empty
                : User.FormatFullName(member.FirstName, member.LastName),
            User.FormatInitials(member.FirstName, member.LastName));

    private static int Count(IEnumerable<DashboardOpenGroupRow> groups) => groups.Sum(g => g.Count);
}
