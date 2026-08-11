namespace Aurora.Flowboard.Application.Components.GetByProject;

public sealed record ComponentResponse(
    Guid Id,
    string Name,
    ComponentStatus Status,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
