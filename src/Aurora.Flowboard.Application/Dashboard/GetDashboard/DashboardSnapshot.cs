namespace Aurora.Flowboard.Application.Dashboard.GetDashboard;

// Everything GetDashboardHandler reads, in the shape DashboardCalculator needs.
internal sealed record DashboardSnapshot(
    IReadOnlyCollection<DashboardProjectRow> Projects,
    IReadOnlyCollection<DashboardStateRow> States,
    IReadOnlyCollection<DashboardOpenGroupRow> OpenGroups,
    IReadOnlyCollection<DashboardMilestoneRow> Milestones,
    IReadOnlyDictionary<Guid, int> ClosedByMilestone,
    int ClosedLast7Days,
    DashboardOldestInFlightResponse? OldestInFlight,
    IReadOnlyCollection<DashboardMemberRow> Members)
{
    public static DashboardSnapshot Empty { get; } = new(
        Array.Empty<DashboardProjectRow>(),
        Array.Empty<DashboardStateRow>(),
        Array.Empty<DashboardOpenGroupRow>(),
        Array.Empty<DashboardMilestoneRow>(),
        new Dictionary<Guid, int>(),
        0,
        null,
        Array.Empty<DashboardMemberRow>());
}

// One of my projects (WhereMyProjects).
internal sealed record DashboardProjectRow(Guid Id, string Name, string Color, ProjectKind Kind, ProjectStatus Status);

// An Active flow state of one of my projects.
internal sealed record DashboardStateRow(Guid Id, Guid ProjectId, string Name, string Color, int SortOrder);

// Open work items of my active projects that share every key; Count is how many there are.
internal sealed record DashboardOpenGroupRow(
    Guid FlowStateId,
    Guid? MilestoneId,
    bool IsMine,
    bool IsUnassigned,
    bool IsCriticalHigh,
    int AgeBand,
    int Count);

// An Active milestone of one of my active projects.
internal sealed record DashboardMilestoneRow(Guid Id, Guid ProjectId, string Name, string Color, DateOnly? TargetEndDate, DateTime CreatedOnUtc);

// How many Completed work items a milestone has.
internal sealed record DashboardMilestoneCountRow(Guid MilestoneId, int Count);

// A member of one of my projects; the names are null when the user row is missing.
internal sealed record DashboardMemberRow(Guid ProjectId, Guid UserId, string? FirstName, string? LastName);
