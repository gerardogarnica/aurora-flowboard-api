namespace Aurora.Flowboard.Application.Projects.GetFlow;

public sealed record GetProjectFlowQuery(Guid ProjectId) : IQuery<ProjectFlowResponse>;
