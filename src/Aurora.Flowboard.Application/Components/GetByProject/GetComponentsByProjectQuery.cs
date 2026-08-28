namespace Aurora.Flowboard.Application.Components.GetByProject;

public sealed record GetComponentsByProjectQuery(Guid ProjectId)
    : IQuery<IReadOnlyCollection<ComponentResponse>>;
