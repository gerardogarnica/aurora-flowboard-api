using Aurora.Flowboard.Domain.Components;
using Aurora.Flowboard.Domain.Milestones;
using Aurora.Flowboard.Domain.Projects.Events;
using Aurora.Flowboard.Domain.Users;
using Aurora.Flowboard.Domain.WorkItems;

namespace Aurora.Flowboard.Domain.Projects;

public sealed class Project : BaseEntity
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;
    public const int MaxActiveFlowStates = 10;

    private static readonly Dictionary<ProjectStatus, ProjectStatus[]> ContinuousTransitions = new()
    {
        [ProjectStatus.Active] = [ProjectStatus.Maintenance, ProjectStatus.Archived],
        [ProjectStatus.Maintenance] = [ProjectStatus.Active, ProjectStatus.Archived],
        [ProjectStatus.Archived] = []
    };

    private static readonly Dictionary<ProjectStatus, ProjectStatus[]> TimeboxedTransitions = new()
    {
        [ProjectStatus.Active] = [ProjectStatus.Completed, ProjectStatus.Archived],
        [ProjectStatus.Completed] = [ProjectStatus.Archived],
        [ProjectStatus.Archived] = []
    };

    private readonly List<ProjectMember> _members = [];
    private readonly List<ProjectChangeLog> _changeLogs = [];
    private readonly List<FlowState> _flowStates = [];
    private readonly List<FlowTransition> _flowTransitions = [];
    private readonly List<Component> _components = [];
    private readonly List<Milestone> _milestones = [];
    private readonly List<WorkItem> _workItems = [];

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public ProjectCode Prefix { get; private set; }
    public ProjectKind Kind { get; private set; }
    public Color Color { get; private set; }
    public ProjectStatus Status { get; private set; }
    public int WorkItemCounter { get; private set; }
    public DateTime LastActivityDate { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public User Creator { get; init; } = null!; // Navigation property

    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<ProjectChangeLog> ChangeLogs => _changeLogs.AsReadOnly();
    public IReadOnlyCollection<FlowState> FlowStates => _flowStates.AsReadOnly();
    public IReadOnlyCollection<FlowTransition> FlowTransitions => _flowTransitions.AsReadOnly();
    public IReadOnlyCollection<Component> Components => _components.AsReadOnly();
    public IReadOnlyCollection<Milestone> Milestones => _milestones.AsReadOnly();
    public IReadOnlyCollection<WorkItem> WorkItems => _workItems.AsReadOnly();

    private bool IsModifiable => Status is ProjectStatus.Active or ProjectStatus.Maintenance;

    private Project() : base(Guid.Empty) { } // EF Core

    private Project(
        Guid id,
        string name,
        string? description,
        ProjectCode prefix,
        ProjectKind kind,
        Color color,
        Guid createdBy,
        DateTime createdOnUtc) : base(id)
    {
        Name = name;
        Description = description;
        Prefix = prefix;
        Kind = kind;
        Color = color;
        WorkItemCounter = 0;
        LastActivityDate = createdOnUtc;
        Status = ProjectStatus.Active;
        CreatedBy = createdBy;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<Project> Create(
        string name,
        string? description,
        ProjectCode prefix,
        ProjectKind kind,
        Color color,
        User createdBy,
        DateTime createdOnUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<Project>(ProjectErrors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<Project>(ProjectErrors.NameTooLong);
        }

        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail<Project>(ProjectErrors.DescriptionTooLong);
        }

        var project = new Project(
            Guid.NewGuid(),
            name.Trim(),
            description?.Trim(),
            prefix,
            kind,
            color,
            createdBy.Id,
            createdOnUtc);

        ProjectMember creatorMember = ProjectMember.Create(project.Id, createdBy.Id, ProjectRole.Admin, createdOnUtc);
        project._members.Add(creatorMember);

        project._changeLogs.Add(ProjectChangeLog.Create(project, createdBy, ProjectChangeType.Created, null, createdOnUtc));

        project.AddDomainEvent(new ProjectCreatedDomainEvent(project.Id));

        return project;
    }

    public Result Update(
        string name,
        string? description,
        Color color,
        User changedBy,
        DateTime updatedOnUtc)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanUpdateProject);
        }

        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(ProjectErrors.NameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail(ProjectErrors.NameTooLong);
        }

        if (description?.Length > MaxDescriptionLength)
        {
            return Result.Fail(ProjectErrors.DescriptionTooLong);
        }

        Name = name.Trim();
        Description = description?.Trim();
        Color = color;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(ProjectChangeLog.Create(this, changedBy, ProjectChangeType.Updated, null, updatedOnUtc));

        AddDomainEvent(new ProjectUpdatedDomainEvent(Id));

        return Result.Ok();
    }

    public Result ChangeKind(ProjectKind newKind, User changedBy, DateTime updatedOnUtc)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanChangeKind);
        }

        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (newKind == Kind)
        {
            return Result.Fail(ProjectErrors.KindUnchanged);
        }

        ProjectKind oldKind = Kind;
        Kind = newKind;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(ProjectChangeLog.Create(this, changedBy, ProjectChangeType.KindChanged, null, updatedOnUtc, newKind: newKind));

        AddDomainEvent(new ProjectKindChangedDomainEvent(Id, oldKind, newKind));

        return Result.Ok();
    }

    public Result ChangeStatus(ProjectStatus newStatus, User changedBy, DateTime updatedOnUtc)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanChangeStatus);
        }

        if (Status == newStatus)
        {
            return Result.Fail(ProjectErrors.InvalidStatusTransition);
        }

        if (!IsValidTransition(Status, newStatus))
        {
            return Result.Fail(ProjectErrors.InvalidStatusTransition);
        }

        ProjectStatus oldStatus = Status;
        Status = newStatus;
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(ProjectChangeLog.Create(this, changedBy, ProjectChangeType.StatusChanged, null, updatedOnUtc, newStatus));

        AddDomainEvent(new ProjectStatusChangedDomainEvent(Id, oldStatus, newStatus));

        return Result.Ok();
    }

    public Result AddMember(User user, ProjectRole role, User changedBy, DateTime joinedOnUtc)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanAddMembers);
        }

        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (!user.IsActive)
        {
            return Result.Fail(UserErrors.Inactive);
        }

        if (_members.Any(m => m.UserId == user.Id))
        {
            return Result.Fail(ProjectErrors.MemberAlreadyExists);
        }

        ProjectMember member = ProjectMember.Create(Id, user.Id, role, joinedOnUtc);
        _members.Add(member);

        _changeLogs.Add(ProjectChangeLog.Create(this, changedBy, ProjectChangeType.MemberAdded, user.Id, joinedOnUtc));

        AddDomainEvent(new ProjectMemberAddedDomainEvent(Id, user.Id, role));

        return Result.Ok();
    }

    public Result RemoveMember(Guid userId, User changedBy, DateTime updatedOnUtc)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanRemoveMembers);
        }

        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        ProjectMember? member = _members.FirstOrDefault(m => m.UserId == userId);

        if (member is null)
        {
            return Result.Fail(ProjectErrors.MemberNotFound);
        }

        if (member.Role == ProjectRole.Admin && _members.Count(m => m.Role == ProjectRole.Admin) == 1)
        {
            return Result.Fail(ProjectErrors.CannotRemoveLastAdmin);
        }

        _members.Remove(member);
        UpdatedOnUtc = updatedOnUtc;

        _changeLogs.Add(ProjectChangeLog.Create(this, changedBy, ProjectChangeType.MemberRemoved, userId, updatedOnUtc));

        AddDomainEvent(new ProjectMemberRemovedDomainEvent(Id, userId));

        return Result.Ok();
    }

    public int IncrementWorkItemCounter()
    {
        WorkItemCounter++;
        return WorkItemCounter;
    }

    public bool CanModifyFlowStates() => IsModifiable;

    public bool CanAddOrUpdateWorkItem() => IsModifiable;

    public Result AddFlowState(
        string name,
        FlowStateCategory category,
        Color color,
        IReadOnlyCollection<ProjectRole> allowedRoles,
        User changedBy)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanModifyFlow);
        }

        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        if (_flowStates.Any(s => s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Fail(ProjectErrors.DuplicateFlowStateName);
        }

        if (category == FlowStateCategory.Active && _flowStates.Count(s => s.Category == FlowStateCategory.Active) >= MaxActiveFlowStates)
        {
            return Result.Fail(ProjectErrors.MaxActiveFlowStatesReached);
        }

        List<FlowState> activeStates = [.. _flowStates.Where(s => s.Category == FlowStateCategory.Active)];
        int nextActiveOrder = activeStates.Count > 0 ? activeStates.Max(s => s.SortOrder) + 1 : 1;
        int sortOrder = category == FlowStateCategory.Active ? nextActiveOrder : 0;

        Result<FlowState> stateResult = FlowState.Create(this, name, category, sortOrder, color);

        if (!stateResult.IsSuccessful)
        {
            return Result.Fail(stateResult.Error);
        }

        FlowState newState = stateResult.Value;
        _flowStates.Add(newState);

        AddDomainEvent(new FlowStateAddedDomainEvent(Id, newState.Id));

        AddFlowStateTransitions(newState, allowedRoles);

        return Result.Ok();
    }

    public Result RemoveFlowState(Guid flowStateId, User changedBy)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanModifyFlow);
        }

        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        FlowState? state = _flowStates.FirstOrDefault(s => s.Id == flowStateId);

        if (state is null)
        {
            return Result.Fail(ProjectErrors.FlowStateNotFound);
        }

        if (state.Category == FlowStateCategory.Completed && _flowStates.Count(s => s.Category == FlowStateCategory.Completed) == 1)
        {
            return Result.Fail(ProjectErrors.LastCompletedFlowState);
        }

        if (state.Category == FlowStateCategory.Cancelled && _flowStates.Count(s => s.Category == FlowStateCategory.Cancelled) == 1)
        {
            return Result.Fail(ProjectErrors.LastCancelledFlowState);
        }

        FlowState? previousActiveState = null;
        FlowState? nextActiveState = null;
        IReadOnlyCollection<ProjectRole> bridgeRoles = [];

        if (state.Category == FlowStateCategory.Active)
        {
            previousActiveState = _flowStates.FirstOrDefault(s => s.Category == FlowStateCategory.Active && s.SortOrder == state.SortOrder - 1);
            nextActiveState = _flowStates.FirstOrDefault(s => s.Category == FlowStateCategory.Active && s.SortOrder == state.SortOrder + 1);

            if (previousActiveState is not null)
            {
                bridgeRoles = _flowTransitions
                    .FirstOrDefault(t => t.FromStateId == previousActiveState.Id && t.ToStateId == flowStateId)
                    ?.AllowedRoles ?? [];
            }
        }

        _flowTransitions.RemoveAll(t => t.FromStateId == flowStateId || t.ToStateId == flowStateId);
        _flowStates.Remove(state);

        if (state.Category == FlowStateCategory.Active)
        {
            foreach (FlowState successor in _flowStates.Where(s => s.Category == FlowStateCategory.Active && s.SortOrder > state.SortOrder))
            {
                successor.DecrementSortOrder();
            }

            if (previousActiveState is not null && nextActiveState is not null)
            {
                _ = AddFlowTransition(previousActiveState, nextActiveState, bridgeRoles);
            }
        }

        return Result.Ok();
    }

    public Result AddFlowTransitionRole(Guid transitionId, ProjectRole role, User changedBy)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanModifyFlow);
        }

        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        FlowTransition? transition = _flowTransitions.FirstOrDefault(t => t.Id == transitionId);

        if (transition is null)
        {
            return Result.Fail(ProjectErrors.FlowTransitionNotFound);
        }

        return transition.AddAllowedRole(role);
    }

    public Result RemoveFlowTransitionRole(Guid transitionId, ProjectRole role, User changedBy)
    {
        if (!IsAdmin(changedBy.Id))
        {
            return Result.Fail(ProjectErrors.OnlyAdminCanModifyFlow);
        }

        if (!IsModifiable)
        {
            return Result.Fail(ProjectErrors.OperationNotAllowedInCurrentStatus);
        }

        FlowTransition? transition = _flowTransitions.FirstOrDefault(t => t.Id == transitionId);

        if (transition is null)
        {
            return Result.Fail(ProjectErrors.FlowTransitionNotFound);
        }

        return transition.RemoveAllowedRole(role);
    }

    private void AddFlowStateTransitions(FlowState newState, IReadOnlyCollection<ProjectRole> allowedRoles)
    {
        switch (newState.Category)
        {
            case FlowStateCategory.Active:
            default:
                {
                    FlowState? previousState = _flowStates
                        .FirstOrDefault(s => s.SortOrder == newState.SortOrder - 1 && s.Category == FlowStateCategory.Active);
                    if (previousState is not null)
                    {
                        _ = AddFlowTransition(previousState, newState, allowedRoles);
                        _ = AddFlowTransition(newState, previousState, allowedRoles);
                    }

                    RerouteCompletedTransitionsToNewActiveFlowState(newState);

                    break;
                }

            case FlowStateCategory.Completed:
                {
                    FlowState? lastActiveState = _flowStates
                        .Where(s => s.Category == FlowStateCategory.Active && s.Id != newState.Id)
                        .MaxBy(s => s.SortOrder);
                    if (lastActiveState is not null)
                    {
                        _ = AddFlowTransition(lastActiveState, newState, allowedRoles);
                    }

                    break;
                }

            case FlowStateCategory.Cancelled:
                {
                    foreach (FlowState activeState in _flowStates.Where(s => s.Category == FlowStateCategory.Active && s.Id != newState.Id))
                    {
                        _ = AddFlowTransition(activeState, newState, allowedRoles);
                    }

                    break;
                }
        }
    }

    private void RerouteCompletedTransitionsToNewActiveFlowState(FlowState newActiveState)
    {
        List<Guid> completedStateIds = [.. _flowStates
            .Where(s => s.Category == FlowStateCategory.Completed)
            .Select(s => s.Id)];

        if (completedStateIds.Count == 0)
        {
            return;
        }

        List<FlowTransition> transitionsToReroute = [.. _flowTransitions.Where(t => completedStateIds.Contains(t.ToStateId))];

        foreach (FlowTransition transition in transitionsToReroute)
        {
            FlowState completedState = _flowStates.First(s => s.Id == transition.ToStateId);
            IReadOnlyCollection<ProjectRole> existingRoles = transition.AllowedRoles;

            _flowTransitions.Remove(transition);
            _ = AddFlowTransition(newActiveState, completedState, existingRoles);
        }
    }

    private Result AddFlowTransition(FlowState fromState, FlowState toState, IReadOnlyCollection<ProjectRole> allowedRoles)
    {
        if (!_flowStates.Any(s => s.Id == fromState.Id))
        {
            return Result.Fail(ProjectErrors.FlowTransitionFromStateNotFound);
        }

        if (!_flowStates.Any(s => s.Id == toState.Id))
        {
            return Result.Fail(ProjectErrors.FlowTransitionToStateNotFound);
        }

        if (_flowTransitions.Any(t => t.FromStateId == fromState.Id && t.ToStateId == toState.Id))
        {
            return Result.Fail(ProjectErrors.FlowTransitionAlreadyExists);
        }

        FlowTransition transition = FlowTransition.Create(this, fromState, toState, allowedRoles);
        _flowTransitions.Add(transition);

        AddDomainEvent(new FlowTransitionAddedDomainEvent(Id, fromState.Id, toState.Id));

        return Result.Ok();
    }

    internal void RegisterComponent(Component component) =>
        _components.Add(component);

    internal void RegisterMilestone(Milestone milestone) =>
        _milestones.Add(milestone);

    internal bool IsAdmin(Guid userId) =>
        _members.Any(m => m.UserId == userId && m.Role == ProjectRole.Admin);

    internal bool IsMember(Guid userId) =>
        _members.Any(m => m.UserId == userId);

    internal ProjectRole? GetRole(Guid userId) =>
        _members.FirstOrDefault(m => m.UserId == userId)?.Role;

    internal FlowState? GetInitialFlowState() =>
        _flowStates
            .Where(s => s.Category == FlowStateCategory.Active)
            .MinBy(s => s.SortOrder);

    internal FlowTransition? FindFlowTransition(Guid fromStateId, Guid toStateId) =>
        _flowTransitions.FirstOrDefault(t => t.FromStateId == fromStateId && t.ToStateId == toStateId);

    private bool IsValidTransition(ProjectStatus from, ProjectStatus to)
    {
        Dictionary<ProjectStatus, ProjectStatus[]> transitions =
            Kind is ProjectKind.Product or ProjectKind.Internal ? ContinuousTransitions : TimeboxedTransitions;

        return transitions.TryGetValue(from, out ProjectStatus[]? targets) && targets.Contains(to);
    }
}
