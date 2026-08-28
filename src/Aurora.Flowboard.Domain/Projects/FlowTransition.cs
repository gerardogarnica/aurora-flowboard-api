namespace Aurora.Flowboard.Domain.Projects;

public sealed class FlowTransition
{
    private readonly List<ProjectRole> _allowedRoles = [];

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid FromStateId { get; private set; }
    public Guid ToStateId { get; private set; }
    public IReadOnlyCollection<ProjectRole> AllowedRoles => _allowedRoles.AsReadOnly();

    private FlowTransition() { } // EF Core

    private FlowTransition(Guid id, Guid projectId, Guid fromStateId, Guid toStateId, IReadOnlyCollection<ProjectRole> allowedRoles)
    {
        Id = id;
        ProjectId = projectId;
        FromStateId = fromStateId;
        ToStateId = toStateId;
        _allowedRoles = [.. allowedRoles];
    }

    internal static FlowTransition Create(Project project, FlowState fromState, FlowState toState, IReadOnlyCollection<ProjectRole> allowedRoles) =>
        new(Guid.NewGuid(), project.Id, fromState.Id, toState.Id, allowedRoles);

    internal Result AddAllowedRole(ProjectRole role)
    {
        if (_allowedRoles.Contains(role))
        {
            return Result.Fail(ProjectErrors.FlowTransitionRoleAlreadyAllowed);
        }

        _allowedRoles.Add(role);

        return Result.Ok();
    }

    internal Result RemoveAllowedRole(ProjectRole role)
    {
        if (!_allowedRoles.Contains(role))
        {
            return Result.Fail(ProjectErrors.FlowTransitionRoleNotAllowed);
        }

        _allowedRoles.Remove(role);

        return Result.Ok();
    }
}
