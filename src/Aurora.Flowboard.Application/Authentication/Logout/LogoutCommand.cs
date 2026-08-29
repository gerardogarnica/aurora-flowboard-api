namespace Aurora.Flowboard.Application.Authentication.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand;
