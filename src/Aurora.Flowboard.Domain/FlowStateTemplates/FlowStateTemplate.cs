using Aurora.Flowboard.Domain.FlowStateTemplates.Events;
using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Domain.FlowStateTemplates;

public sealed class FlowStateTemplate : BaseEntity
{
    public const int MaxActiveStates = Project.MaxActiveFlowStates;

    private readonly List<TemplateFlowState> _states = [];

    public ProjectKind Kind { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public IReadOnlyCollection<TemplateFlowState> States => _states.AsReadOnly();

    private FlowStateTemplate() : base(Guid.Empty) { } // EF Core

    private FlowStateTemplate(
        Guid id,
        ProjectKind kind,
        Guid createdBy,
        DateTime createdOnUtc) : base(id)
    {
        Kind = kind;
        CreatedBy = createdBy;
        CreatedOnUtc = createdOnUtc;
    }

    public static Result<FlowStateTemplate> Create(ProjectKind kind, Guid createdBy, DateTime createdOnUtc)
    {
        var template = new FlowStateTemplate(Guid.NewGuid(), kind, createdBy, createdOnUtc);

        template.AddDomainEvent(new FlowStateTemplateCreatedDomainEvent(template.Id, template.Kind));

        return template;
    }

    public Result AddState(string name, FlowStateCategory category, Color color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(FlowStateTemplateErrors.StateNameRequired);
        }

        if (name.Length > TemplateFlowState.MaxNameLength)
        {
            return Result.Fail(FlowStateTemplateErrors.StateNameTooLong);
        }

        if (_states.Any(s => s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Fail(FlowStateTemplateErrors.DuplicateStateName);
        }

        if (category == FlowStateCategory.Active && ActiveStateCount() >= MaxActiveStates)
        {
            return Result.Fail(FlowStateTemplateErrors.MaxActiveStatesReached);
        }

        int sortOrder = category == FlowStateCategory.Active ? NextActiveSortOrder() : 0;

        Result<TemplateFlowState> stateResult = TemplateFlowState.Create(this, name, sortOrder, category, color);

        if (!stateResult.IsSuccessful)
        {
            return Result.Fail(stateResult.Error);
        }

        TemplateFlowState newState = stateResult.Value;
        _states.Add(newState);

        AddDomainEvent(new TemplateFlowStateAddedDomainEvent(Id, newState.Id));

        return Result.Ok();
    }

    public Result UpdateState(Guid stateId, string name, Color color)
    {
        TemplateFlowState? state = _states.FirstOrDefault(s => s.Id == stateId);

        if (state is null)
        {
            return Result.Fail(FlowStateTemplateErrors.StateNotFound);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(FlowStateTemplateErrors.StateNameRequired);
        }

        if (name.Length > TemplateFlowState.MaxNameLength)
        {
            return Result.Fail(FlowStateTemplateErrors.StateNameTooLong);
        }

        if (_states.Any(s => s.Id != stateId && s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Fail(FlowStateTemplateErrors.DuplicateStateName);
        }

        Result renameResult = state.Rename(name);

        if (!renameResult.IsSuccessful)
        {
            return renameResult;
        }

        state.ChangeColor(color);

        AddDomainEvent(new TemplateFlowStateUpdatedDomainEvent(Id, state.Id));

        return Result.Ok();
    }

    public Result RemoveState(Guid stateId)
    {
        TemplateFlowState? state = _states.FirstOrDefault(s => s.Id == stateId);

        if (state is null)
        {
            return Result.Fail(FlowStateTemplateErrors.StateNotFound);
        }

        _states.Remove(state);

        if (state.Category == FlowStateCategory.Active)
        {
            DecrementSubsequentActiveStates(state.SortOrder);
        }

        AddDomainEvent(new TemplateFlowStateRemovedDomainEvent(Id, state.Id));

        return Result.Ok();
    }

    public Result ReorderStates(IReadOnlyList<Guid> orderedActiveStateIds)
    {
        List<TemplateFlowState> activeStates = [.. _states.Where(s => s.Category == FlowStateCategory.Active)];

        bool isSameSet = activeStates.Count == orderedActiveStateIds.Count
            && activeStates.All(s => orderedActiveStateIds.Contains(s.Id))
            && orderedActiveStateIds.Distinct().Count() == orderedActiveStateIds.Count;

        if (!isSameSet)
        {
            return Result.Fail(FlowStateTemplateErrors.InvalidReorderSet);
        }

        for (int index = 0; index < orderedActiveStateIds.Count; index++)
        {
            TemplateFlowState state = activeStates.First(s => s.Id == orderedActiveStateIds[index]);
            state.SetSortOrder(index + 1);
        }

        AddDomainEvent(new TemplateFlowStatesReorderedDomainEvent(Id));

        return Result.Ok();
    }

    private int ActiveStateCount() => _states.Count(s => s.Category == FlowStateCategory.Active);

    private int NextActiveSortOrder()
    {
        List<TemplateFlowState> activeStates = [.. _states.Where(s => s.Category == FlowStateCategory.Active)];
        return activeStates.Count > 0 ? activeStates.Max(s => s.SortOrder) + 1 : 1;
    }

    private void DecrementSubsequentActiveStates(int sortOrder)
    {
        foreach (TemplateFlowState successor in _states.Where(s => s.Category == FlowStateCategory.Active && s.SortOrder > sortOrder))
        {
            successor.DecrementSortOrder();
        }
    }
}
