namespace Aurora.Flowboard.Application.Projects.GetAll;

public sealed record GetAllProjectsQuery : IQuery<IReadOnlyCollection<ProjectSummaryResponse>>;
