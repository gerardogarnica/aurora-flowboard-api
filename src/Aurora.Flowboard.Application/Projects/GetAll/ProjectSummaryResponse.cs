namespace Aurora.Flowboard.Application.Projects.GetAll;

public sealed record ProjectSummaryResponse(
    Guid ProjectId,
    string Name,
    string? Description,
    DateOnly? EstimatedCompletionDate,
    ProjectStatus Status,
    int MemberCount,
    IReadOnlyCollection<ProjectFlowSummaryResponse> Flows);

public sealed record ProjectFlowSummaryResponse(
    Guid FlowId,
    string Name,
    bool IsDefault,
    bool IsActive);
