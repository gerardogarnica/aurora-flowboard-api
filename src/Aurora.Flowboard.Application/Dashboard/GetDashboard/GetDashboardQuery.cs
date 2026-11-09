namespace Aurora.Flowboard.Application.Dashboard.GetDashboard;

// No parameters: the scope is the current user (IUserContext), so there is nothing to validate.
public sealed record GetDashboardQuery : IQuery<DashboardResponse>;
