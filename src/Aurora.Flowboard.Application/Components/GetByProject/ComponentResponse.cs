namespace Aurora.Flowboard.Application.Components.GetByProject;

public sealed record ComponentResponse(
    Guid Id,
    string Name,
    ComponentStatus Status,
    Guid CreatedBy,
    DateTime CreatedOnUtc,
    DateTime? UpdatedOnUtc);
