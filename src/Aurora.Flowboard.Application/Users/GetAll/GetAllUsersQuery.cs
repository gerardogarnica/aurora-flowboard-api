namespace Aurora.Flowboard.Application.Users.GetAll;

public sealed record GetAllUsersQuery : IQuery<IReadOnlyCollection<UserSummaryResponse>>;
