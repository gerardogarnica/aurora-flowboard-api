using Aurora.Flowboard.Domain.Flows.Events;
using Aurora.Flowboard.Domain.Projects;
using Aurora.Flowboard.Domain.Users;

namespace Aurora.Flowboard.Domain.Flows;

public sealed class Flow : BaseEntity
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;
    public const int MaxActiveStates = 10;

    private readonly List<FlowState> _states = [];
    private readonly List<FlowTransition> _transitions = [];

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid ProjectId { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public Project Project { get; init; } = null!; // Navigation property
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
        if (!project.CanAddOrUpdateFlow())
        {
            return Result.Fail<Flow>(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<Flow>(FlowErrors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<Flow>(FlowErrors.NameTooLong);
        }

        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail<Flow>(FlowErrors.DescriptionTooLong);
        }

        var flow = new Flow(
            Guid.NewGuid(),
            name.Trim(),
            description?.Trim(),
            project.Id,
            isDefault,
            createdOnUtc)
        {
            Project = project
        };

        flow.AddDomainEvent(new FlowCreatedDomainEvent(flow.Id));

        return flow;
    }

    public Result Update(string name, string? description, User changedBy, DateTime updatedOnUtc)
    {
        if (!Project.IsAdmin(changedBy.Id))
        {
            return Result.Fail(FlowErrors.OnlyAdminCanModifyFlow);
        }

        if (!IsActive)
        {
            return Result.Fail(FlowErrors.Deactivated);
        }

        if (!Project.CanAddOrUpdateFlow())
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(FlowErrors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail(FlowErrors.NameTooLong);
        }

        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail<Flow>(FlowErrors.DescriptionTooLong);
        }

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedOnUtc = updatedOnUtc;

        AddDomainEvent(new FlowUpdatedDomainEvent(Id));

        return Result.Ok();
    }

    public Result Deactivate(User changedBy, DateTime updatedOnUtc)
    {
        if (!Project.IsAdmin(changedBy.Id))
        {
            return Result.Fail(FlowErrors.OnlyAdminCanModifyFlow);
        }

        if (!IsActive)
        {
            return Result.Fail(FlowErrors.AlreadyDeactivated);
        }

        if (IsDefault)
        {
            return Result.Fail(FlowErrors.IsDefault);
        }

        if (!Project.CanAddOrUpdateFlow())
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        IsActive = false;
        UpdatedOnUtc = updatedOnUtc;

        return Result.Ok();
    }

    public Result AddState(string name, FlowStateCategory category, IReadOnlyCollection<ProjectRole> allowedRoles, User changedBy)
    {
        if (!Project.IsAdmin(changedBy.Id))
        {
            return Result.Fail(FlowErrors.OnlyAdminCanModifyFlow);
        }

        if (!IsActive)
        {
            return Result.Fail(FlowErrors.Deactivated);
        }

        if (!Project.CanAddOrUpdateFlow())
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (_states.Any(s => s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Fail(FlowErrors.DuplicateStateName);
        }

        if (category == FlowStateCategory.Active && _states.Count(s => s.Category == FlowStateCategory.Active) >= MaxActiveStates)
        {
            return Result.Fail(FlowErrors.MaxActiveStatesReached);
        }

        int nextActiveOrder = _states.Any(s => s.Category == FlowStateCategory.Active) ? _states.Max(s => s.SortOrder) + 1 : 1;
        int sortOrder = category == FlowStateCategory.Active ? nextActiveOrder : 0;

        Result<FlowState> stateResult = FlowState.Create(this, name, sortOrder, category);

        if (!stateResult.IsSuccessful)
        {
            return Result.Fail(stateResult.Error);
        }

        FlowState newState = stateResult.Value;
        _states.Add(newState);

        AddDomainEvent(new FlowStateAddedDomainEvent(Id, newState.Id));

        AddStateTransitions(newState, allowedRoles);

        return Result.Ok();
    }

    private void AddStateTransitions(FlowState newState, IReadOnlyCollection<ProjectRole> allowedRoles)
    {
        switch (newState.Category)
        {
            case FlowStateCategory.Active:
            default:
                {
                    FlowState? previousState = _states
                        .FirstOrDefault(s => s.SortOrder == newState.SortOrder - 1 && s.Category == FlowStateCategory.Active);
                    if (previousState is not null)
                    {
                        _ = AddTransition(previousState, newState, allowedRoles);
                        _ = AddTransition(newState, previousState, allowedRoles);
                    }

                    RerouteCompletedTransitionsToNewActiveState(newState);

                    break;
                }

            case FlowStateCategory.Completed:
                {
                    FlowState? lastActiveState = _states
                        .Where(s => s.Category == FlowStateCategory.Active && s.Id != newState.Id)
                        .MaxBy(s => s.SortOrder);
                    if (lastActiveState is not null)
                    {
                        _ = AddTransition(lastActiveState, newState, allowedRoles);
                    }

                    break;
                }

            case FlowStateCategory.Cancelled:
                {
                    foreach (FlowState activeState in _states.Where(s => s.Category == FlowStateCategory.Active && s.Id != newState.Id))
                    {
                        _ = AddTransition(activeState, newState, allowedRoles);
                    }

                    break;
                }
        }
    }

    private void RerouteCompletedTransitionsToNewActiveState(FlowState newActiveState)
    {
        List<Guid> completedStateIds = [.. _states
            .Where(s => s.Category == FlowStateCategory.Completed)
            .Select(s => s.Id)];

        if (completedStateIds.Count == 0)
        {
            return;
        }

        List<FlowTransition> transitionsToReroute = [.. _transitions.Where(t => completedStateIds.Contains(t.ToStateId))];

        foreach (FlowTransition transition in transitionsToReroute)
        {
            FlowState completedState = _states.First(s => s.Id == transition.ToStateId);
            IReadOnlyCollection<ProjectRole> existingRoles = transition.AllowedRoles;

            _transitions.Remove(transition);
            _ = AddTransition(newActiveState, completedState, existingRoles);
        }
    }

    public Result RemoveState(Guid stateId, User changedBy)
    {
        if (!Project.IsAdmin(changedBy.Id))
        {
            return Result.Fail(FlowErrors.OnlyAdminCanModifyFlow);
        }

        if (!IsActive)
        {
            return Result.Fail(FlowErrors.Deactivated);
        }

        if (!Project.CanAddOrUpdateFlow())
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        FlowState? state = _states.FirstOrDefault(s => s.Id == stateId);

        if (state is null)
        {
            return Result.Fail(FlowErrors.StateNotFound);
        }

        if (state.Category == FlowStateCategory.Completed && _states.Count(s => s.Category == FlowStateCategory.Completed) == 1)
        {
            return Result.Fail(FlowErrors.LastCompletedState);
        }

        if (state.Category == FlowStateCategory.Cancelled && _states.Count(s => s.Category == FlowStateCategory.Cancelled) == 1)
        {
            return Result.Fail(FlowErrors.LastCancelledState);
        }

        FlowState? previousActiveState = null;
        FlowState? nextActiveState = null;
        IReadOnlyCollection<ProjectRole> bridgeRoles = [];

        if (state.Category == FlowStateCategory.Active)
        {
            previousActiveState = _states.FirstOrDefault(s => s.Category == FlowStateCategory.Active && s.SortOrder == state.SortOrder - 1);
            nextActiveState = _states.FirstOrDefault(s => s.Category == FlowStateCategory.Active && s.SortOrder == state.SortOrder + 1);

            if (previousActiveState is not null)
            {
                bridgeRoles = _transitions
                    .FirstOrDefault(t => t.FromStateId == previousActiveState.Id && t.ToStateId == stateId)
                    ?.AllowedRoles ?? [];
            }
        }

        _transitions.RemoveAll(t => t.FromStateId == stateId || t.ToStateId == stateId);
        _states.Remove(state);

        if (state.Category == FlowStateCategory.Active)
        {
            foreach (FlowState successor in _states.Where(s => s.Category == FlowStateCategory.Active && s.SortOrder > state.SortOrder))
            {
                successor.DecrementSortOrder();
            }

            if (previousActiveState is not null && nextActiveState is not null)
            {
                _ = AddTransition(previousActiveState, nextActiveState, bridgeRoles);
            }
        }

        return Result.Ok();
    }

    internal FlowState? GetInitialState() =>
        _states
            .Where(s => s.Category == FlowStateCategory.Active)
            .MinBy(s => s.SortOrder);

    internal FlowTransition? FindTransition(Guid fromStateId, Guid toStateId) =>
        _transitions.FirstOrDefault(t => t.FromStateId == fromStateId && t.ToStateId == toStateId);

    public Result AddTransitionRole(Guid transitionId, ProjectRole role, User changedBy)
    {
        if (!Project.IsAdmin(changedBy.Id))
        {
            return Result.Fail(FlowErrors.OnlyAdminCanModifyFlow);
        }

        if (!IsActive)
        {
            return Result.Fail(FlowErrors.Deactivated);
        }

        if (!Project.CanAddOrUpdateFlow())
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        FlowTransition? transition = _transitions.FirstOrDefault(t => t.Id == transitionId);

        if (transition is null)
        {
            return Result.Fail(FlowErrors.TransitionNotFound);
        }

        return transition.AddAllowedRole(role);
    }

    public Result RemoveTransitionRole(Guid transitionId, ProjectRole role, User changedBy)
    {
        if (!Project.IsAdmin(changedBy.Id))
        {
            return Result.Fail(FlowErrors.OnlyAdminCanModifyFlow);
        }

        if (!IsActive)
        {
            return Result.Fail(FlowErrors.Deactivated);
        }

        if (!Project.CanAddOrUpdateFlow())
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        FlowTransition? transition = _transitions.FirstOrDefault(t => t.Id == transitionId);

        if (transition is null)
        {
            return Result.Fail(FlowErrors.TransitionNotFound);
        }

        return transition.RemoveAllowedRole(role);
    }

    private Result AddTransition(FlowState fromState, FlowState toState, IReadOnlyCollection<ProjectRole> allowedRoles)
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

        var transition = FlowTransition.Create(this, fromState, toState, allowedRoles);
        _transitions.Add(transition);

        AddDomainEvent(new FlowTransitionAddedDomainEvent(Id, fromState.Id, toState.Id));

        return Result.Ok();
    }
}
