using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Domain.Flows;

public sealed class FlowTransition
{
    private readonly List<ProjectRole> _allowedRoles = [];

    public Guid Id { get; private set; }
    public Guid FlowId { get; private set; }
    public Guid FromStateId { get; private set; }
    public Guid ToStateId { get; private set; }
    public IReadOnlyCollection<ProjectRole> AllowedRoles => _allowedRoles.AsReadOnly();

    private FlowTransition() { } // EF Core

    private FlowTransition(Guid id, Guid flowId, Guid fromStateId, Guid toStateId, IReadOnlyCollection<ProjectRole> allowedRoles)
    {
        Id = id;
        FlowId = flowId;
        FromStateId = fromStateId;
        ToStateId = toStateId;
        _allowedRoles = [.. allowedRoles];
    }

    internal static FlowTransition Create(Flow flow, FlowState fromState, FlowState toState, IReadOnlyCollection<ProjectRole> allowedRoles) =>
        new(Guid.NewGuid(), flow.Id, fromState.Id, toState.Id, allowedRoles);

    internal Result AddAllowedRole(ProjectRole role)
    {
        if (_allowedRoles.Contains(role))
        {
            return Result.Fail(FlowErrors.TransitionRoleAlreadyAllowed);
        }

        _allowedRoles.Add(role);

        return Result.Ok();
    }

    internal Result RemoveAllowedRole(ProjectRole role)
    {
        if (!_allowedRoles.Contains(role))
        {
            return Result.Fail(FlowErrors.TransitionRoleNotAllowed);
        }

        _allowedRoles.Remove(role);

        return Result.Ok();
    }
}
