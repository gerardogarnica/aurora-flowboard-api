using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Domain.Flows;

public sealed class FlowTransition
{
    public Guid Id { get; private set; }
    public Guid FlowId { get; private set; }
    public Guid FromStateId { get; private set; }
    public Guid ToStateId { get; private set; }
    public ProjectRole AllowedRole { get; private set; }

    private FlowTransition() { } // EF Core

    private FlowTransition(Guid id, Guid flowId, Guid fromStateId, Guid toStateId, ProjectRole allowedRole)
    {
        Id = id;
        FlowId = flowId;
        FromStateId = fromStateId;
        ToStateId = toStateId;
        AllowedRole = allowedRole;
    }

    internal static FlowTransition Create(Flow flow, FlowState fromState, FlowState toState, ProjectRole allowedRole) =>
        new(Guid.NewGuid(), flow.Id, fromState.Id, toState.Id, allowedRole);
}
