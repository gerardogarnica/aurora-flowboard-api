using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Domain.FlowStateTemplates;

public sealed class TemplateFlowState
{
    public const int MaxNameLength = 50;

    public Guid Id { get; private set; }
    public Guid FlowStateTemplateId { get; private set; }
    public string Name { get; private set; }
    public int SortOrder { get; private set; }
    public FlowStateCategory Category { get; private set; }
    public Color Color { get; private set; }

    private TemplateFlowState() { } // EF Core

    private TemplateFlowState(
        Guid id,
        Guid flowStateTemplateId,
        string name,
        int sortOrder,
        FlowStateCategory category,
        Color color)
    {
        Id = id;
        FlowStateTemplateId = flowStateTemplateId;
        Name = name;
        SortOrder = sortOrder;
        Category = category;
        Color = color;
    }

    internal static Result<TemplateFlowState> Create(
        FlowStateTemplate template,
        string name,
        int sortOrder,
        FlowStateCategory category,
        Color color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<TemplateFlowState>(FlowStateTemplateErrors.StateNameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<TemplateFlowState>(FlowStateTemplateErrors.StateNameTooLong);
        }

        return new TemplateFlowState(
            Guid.NewGuid(),
            template.Id,
            name.Trim(),
            sortOrder,
            category,
            color);
    }

    internal Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Fail(FlowStateTemplateErrors.StateNameRequired);
        }

        if (newName.Length > MaxNameLength)
        {
            return Result.Fail(FlowStateTemplateErrors.StateNameTooLong);
        }

        Name = newName.Trim();

        return Result.Ok();
    }

    internal void ChangeColor(Color color) => Color = color;

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    internal void DecrementSortOrder() => SortOrder--;
}
