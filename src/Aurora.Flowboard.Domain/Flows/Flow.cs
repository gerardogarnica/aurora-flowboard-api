using Aurora.Flowboard.Domain.Flows.Events;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Domain.Flows;

public sealed class Flow : BaseEntity
{
    private const int MaxNameLength = 100;

    private readonly List<FlowState> _states = [];
    private readonly List<FlowTransition> _transitions = [];

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid ProjectId { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public IReadOnlyCollection<FlowState> States => _states.AsReadOnly();
    public IReadOnlyCollection<FlowTransition> Transitions => _transitions.AsReadOnly();

    private Flow() : base(Guid.Empty) { } // EF Core

    private Flow(
        Guid id,
        string name,
        string? description,
        Guid projectId,
        bool isDefault,
        DateTime createdOnUtc) : base(id)
    {
        Name = name;
        Description = description;
        ProjectId = projectId;
        IsDefault = isDefault;
        IsActive = true;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<Flow> Create(
        string name,
        string? description,
        Project project,
        bool isDefault,
        DateTime createdOnUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<Flow>(FlowErrors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<Flow>(FlowErrors.NameTooLong);
        }

        if (!project.CanAddOrUpdateFlow())
        {
            return Result.Fail<Flow>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        var flow = new Flow(
            Guid.NewGuid(),
            name.Trim(),
            description?.Trim(),
            project.Id,
            isDefault,
            createdOnUtc);

        flow.AddDomainEvent(new FlowCreatedDomainEvent(flow.Id));

        return flow;
    }

    public Result Update(string name, string? description, DateTime updatedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(FlowErrors.Deactivated);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(FlowErrors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail(FlowErrors.NameTooLong);
        }

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new FlowUpdatedDomainEvent(Id));

        return Result.Ok();
    }

    public Result Deactivate(DateTime updatedOnUtc)
    {
        if (!IsActive)
        {
            return Result.Fail(FlowErrors.AlreadyDeactivated);
        }

        IsActive = false;
        UpdatedOnUtc = updatedOnUtc;

        return Result.Ok();
    }

    public Result AddState(string name, FlowStateCategory category)
    {
        if (!IsActive)
        {
            return Result.Fail(FlowErrors.Deactivated);
        }

        if (_states.Any(s => s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Fail(FlowErrors.DuplicateStateName);
        }

        int sortOrder = _states.Count > 0 ? _states.Max(s => s.SortOrder) + 1 : 1;

        Result<FlowState> stateResult = FlowState.Create(this, name, sortOrder, category);

        if (!stateResult.IsSuccessful)
        {
            return Result.Fail(stateResult.Error);
        }

        _states.Add(stateResult.Value);

        AddDomainEvent(new FlowStateAddedDomainEvent(Id, stateResult.Value.Id));

        return Result.Ok();
    }

    public Result RemoveState(Guid stateId)
    {
        if (!IsActive)
        {
            return Result.Fail(FlowErrors.Deactivated);
        }

        var state = _states.FirstOrDefault(s => s.Id == stateId);

        if (state is null)
        {
            return Result.Fail(FlowErrors.StateNotFound);
        }

        _transitions.RemoveAll(t => t.FromStateId == stateId || t.ToStateId == stateId);
        _states.Remove(state);

        return Result.Ok();
    }

    public Result AddTransition(FlowState fromState, FlowState toState, ProjectRole allowedRole)
    {
        if (!IsActive)
        {
            return Result.Fail(FlowErrors.Deactivated);
        }

        if (!_states.Any(s => s.Id == fromState.Id))
        {
            return Result.Fail(FlowErrors.TransitionFromStateNotFound);
        }

        if (!_states.Any(s => s.Id == toState.Id))
        {
            return Result.Fail(FlowErrors.TransitionToStateNotFound);
        }

        if (_transitions.Any(t => t.FromStateId == fromState.Id && t.ToStateId == toState.Id))
        {
            return Result.Fail(FlowErrors.TransitionAlreadyExists);
        }

        var transition = FlowTransition.Create(this, fromState, toState, allowedRole);
        _transitions.Add(transition);

        AddDomainEvent(new FlowTransitionAddedDomainEvent(Id, fromState.Id, toState.Id));

        return Result.Ok();
    }

    public Result RemoveTransition(Guid transitionId)
    {
        if (!IsActive)
        {
            return Result.Fail(FlowErrors.Deactivated);
        }

        var transition = _transitions.FirstOrDefault(t => t.Id == transitionId);

        if (transition is null)
        {
            return Result.Fail(FlowErrors.TransitionNotFound);
        }

        _transitions.Remove(transition);

        return Result.Ok();
    }
}
