namespace Aurora.Flowboard.Application.Users.GetMySummary;

public sealed record GetMySummaryQuery(Guid UserId) : IQuery<MySummaryResponse>;
