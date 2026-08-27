namespace Aurora.Flowboard.Application.TemplateFlows.GetByKind;

public sealed record TemplateFlowResponse(
    Guid Id,
    ProjectKind Kind,
    IReadOnlyCollection<TemplateFlowStateResponse> States);

public sealed record TemplateFlowStateResponse(
    Guid Id,
    string Name,
    int SortOrder,
    FlowStateCategory Category,
    string Color);
