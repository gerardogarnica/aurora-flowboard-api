using Aurora.Flowboard.Domain.Projects;

namespace Aurora.Flowboard.Domain.TemplateFlows;

public sealed class TemplateFlowState
{
    public const int MaxNameLength = 50;

    public Guid Id { get; private set; }
    public Guid TemplateFlowId { get; private set; }
    public string Name { get; private set; }
    public FlowStateCategory Category { get; private set; }
    public int SortOrder { get; private set; }
    public Color Color { get; private set; }

    private TemplateFlowState() { } // EF Core

    private TemplateFlowState(
        Guid id,
        Guid templateFlowId,
        string name,
        FlowStateCategory category,
        int sortOrder,
        Color color)
    {
        Id = id;
        TemplateFlowId = templateFlowId;
        Name = name;
        Category = category;
        SortOrder = sortOrder;
        Color = color;
    }

    internal static Result<TemplateFlowState> Create(
        TemplateFlow template,
        string name,
        FlowStateCategory category,
        int sortOrder,
        Color color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail<TemplateFlowState>(TemplateFlowErrors.StateNameRequired);
        }

        if (name.Length > MaxNameLength)
        {
            return Result.Fail<TemplateFlowState>(TemplateFlowErrors.StateNameTooLong);
        }

        return new TemplateFlowState(
            Guid.NewGuid(),
            template.Id,
            name.Trim(),
            category,
            sortOrder,
            color);
    }

    internal Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Fail(TemplateFlowErrors.StateNameRequired);
        }

        if (newName.Length > MaxNameLength)
        {
            return Result.Fail(TemplateFlowErrors.StateNameTooLong);
        }

        Name = newName.Trim();

        return Result.Ok();
    }

    internal void ChangeColor(Color color) => Color = color;

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    internal void DecrementSortOrder() => SortOrder--;
}
