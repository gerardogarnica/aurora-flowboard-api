namespace Aurora.Flowboard.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Role) : ICommand<Guid>;
