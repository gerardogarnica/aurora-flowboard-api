namespace Aurora.Flowboard.Application.Projects.GetAll;

public sealed record GetAllProjectsQuery(ProjectStatus? StatusFilter) : IQuery<IReadOnlyCollection<ProjectSummaryResponse>>;
