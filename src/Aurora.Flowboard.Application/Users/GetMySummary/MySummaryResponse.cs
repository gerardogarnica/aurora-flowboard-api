namespace Aurora.Flowboard.Application.Users.GetMySummary;

public sealed record MySummaryResponse(
    MyProfileResponse Me,
    MySummaryCountsResponse Counts,
    IReadOnlyCollection<MyProjectSummaryResponse> Projects);

public sealed record MyProfileResponse(
    Guid UserId,
    string FullName,
    string Initials,
    string Email,
    string Role);

public sealed record MySummaryCountsResponse(
    int Projects,
    int Members,
    int InboxUnread,
    int MyOpenIssues);

public sealed record MyProjectSummaryResponse(
    Guid ProjectId,
    string Name,
    string Color,
    ProjectStatus Status);
