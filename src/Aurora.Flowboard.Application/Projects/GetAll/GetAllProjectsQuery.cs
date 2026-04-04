namespace Aurora.Flowboard.Application.Projects.GetAll;

public sealed record GetAllProjectsQuery(bool IncludeDeactivated) : IQuery<IReadOnlyCollection<ProjectSummaryResponse>>;
