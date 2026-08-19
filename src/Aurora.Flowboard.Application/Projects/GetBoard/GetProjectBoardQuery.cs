namespace Aurora.Flowboard.Application.Projects.GetBoard;

public sealed record GetProjectBoardQuery(Guid ProjectId) : IQuery<IReadOnlyCollection<BoardColumnResponse>>;
