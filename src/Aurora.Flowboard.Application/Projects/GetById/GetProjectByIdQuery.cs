namespace Aurora.Flowboard.Application.Projects.GetById;

public sealed record GetProjectByIdQuery(Guid ProjectId) : IQuery<ProjectResponse>;
