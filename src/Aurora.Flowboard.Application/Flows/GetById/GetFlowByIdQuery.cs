namespace Aurora.Flowboard.Application.Flows.GetById;

public sealed record GetFlowByIdQuery(Guid FlowId) : IQuery<FlowResponse>;
