namespace Aurora.Flowboard.Application.Users.ChangeRole;

public sealed record ChangeUserRoleCommand(Guid UserId, string Role) : ICommand;
