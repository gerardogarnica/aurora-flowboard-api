namespace Aurora.Flowboard.Application.Authentication.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<IdentityToken>;
