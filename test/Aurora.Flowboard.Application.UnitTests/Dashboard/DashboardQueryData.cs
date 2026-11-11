namespace Aurora.Flowboard.Application.UnitTests.Dashboard;

// Rows for DashboardCalculator, which is tested without the handler: each test states its own rows.
internal static class DashboardQueryData
{
    public const string ProjectColor = "#3B82F6";
    public const string StateColor = "#94A3B8";
    public const string MilestoneColor = "#F97316";
    public static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static DashboardProjectRow ProjectRow(string name, ProjectStatus status = ProjectStatus.Active) =>
        new(Guid.NewGuid(), name, ProjectColor, ProjectKind.Client, status);

    public static DashboardStateRow StateRow(DashboardProjectRow project, string name, int sortOrder) =>
        new(Guid.NewGuid(), project.Id, name, StateColor, sortOrder);

    public static DashboardOpenGroupRow GroupRow(
        DashboardStateRow state,
        int count,
        int ageBand = DashboardCalculator.FreshBand,
        bool isMine = false,
        bool isUnassigned = false,
        bool isCriticalHigh = false,
        Guid? milestoneId = null) =>
        new(state.Id, milestoneId, isMine, isUnassigned, isCriticalHigh, ageBand, count);

    public static DashboardMilestoneRow MilestoneRow(DashboardProjectRow project, string name, DateOnly? targetEndDate, DateTime? createdOnUtc = null) =>
        new(Guid.NewGuid(), project.Id, name, MilestoneColor, targetEndDate, createdOnUtc ?? CreatedOnUtc);

    public static DashboardMemberRow MemberRow(DashboardProjectRow project, string firstName, string lastName) =>
        new(project.Id, Guid.NewGuid(), firstName, lastName);

    public static DashboardSnapshot Snapshot(
        IReadOnlyCollection<DashboardProjectRow> projects,
        IReadOnlyCollection<DashboardStateRow> states,
        IReadOnlyCollection<DashboardOpenGroupRow>? openGroups = null,
        IReadOnlyCollection<DashboardMilestoneRow>? milestones = null,
        IReadOnlyDictionary<Guid, int>? closedByMilestone = null,
        int closedLast7Days = 0,
        IReadOnlyCollection<DashboardMemberRow>? members = null) =>
        new(
            projects,
            states,
            openGroups ?? Array.Empty<DashboardOpenGroupRow>(),
            milestones ?? Array.Empty<DashboardMilestoneRow>(),
            closedByMilestone ?? new Dictionary<Guid, int>(),
            closedLast7Days,
            null,
            members ?? Array.Empty<DashboardMemberRow>());
}
