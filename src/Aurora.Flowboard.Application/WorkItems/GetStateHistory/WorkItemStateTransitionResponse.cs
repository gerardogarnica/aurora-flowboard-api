namespace Aurora.Flowboard.Application.WorkItems.GetStateHistory;

public sealed record WorkItemStateTransitionResponse(
    Guid StateTransitionId,
    Guid? FromStateId,
    string? FromStateName,
    Guid ToStateId,
    string ToStateName,
    Guid ChangedById,
    string ChangedByFullName,
    string? Reason,
    DateTime ChangedOnUtc);
