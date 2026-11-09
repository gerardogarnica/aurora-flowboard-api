namespace Aurora.Flowboard.Application.Dashboard.GetDashboard;

public sealed record DashboardResponse(
    DashboardMeResponse Me,
    DashboardTeamResponse Team,
    DashboardAgingResponse Aging,
    IReadOnlyCollection<DashboardMilestoneResponse> Milestones,
    IReadOnlyCollection<DashboardProjectResponse> Portfolio);

public sealed record DashboardMeResponse(
    int Open,
    int Stalled,
    int InFlight,
    int CriticalHigh);

public sealed record DashboardTeamResponse(
    int Stalled,
    int Unassigned,
    int ClosedLast7Days);

public sealed record DashboardAgingResponse(
    IReadOnlyCollection<DashboardAgingBucketResponse> Buckets,
    DashboardOldestInFlightResponse? OldestInFlight);

public sealed record DashboardAgingBucketResponse(
    int From,
    int? To,
    int Count);

public sealed record DashboardOldestInFlightResponse(
    string Code,
    string Title,
    Guid ProjectId,
    string ProjectName,
    DateTime EnteredCurrentStateOnUtc);

public sealed record DashboardMilestoneResponse(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string Name,
    string Color,
    DateOnly? TargetEndDate,
    int LinkedTotal,
    int LinkedClosed,
    int LinkedStalled);

public sealed record DashboardProjectResponse(
    Guid ProjectId,
    string Name,
    string Color,
    ProjectKind Kind,
    ProjectStatus Status,
    int Open,
    int Stalled,
    IReadOnlyCollection<DashboardFlowStateCountResponse> Distribution,
    DashboardActiveMilestoneResponse? ActiveMilestone,
    IReadOnlyCollection<DashboardMemberResponse> Members);

public sealed record DashboardFlowStateCountResponse(
    Guid FlowStateId,
    string Name,
    string Color,
    int Count);

public sealed record DashboardActiveMilestoneResponse(
    Guid Id,
    string Name,
    string Color,
    DateOnly? TargetEndDate,
    int LinkedTotal,
    int LinkedClosed);

public sealed record DashboardMemberResponse(
    Guid UserId,
    string FullName,
    string Initials);
