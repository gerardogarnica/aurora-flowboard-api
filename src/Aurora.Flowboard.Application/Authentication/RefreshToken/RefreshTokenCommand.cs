namespace Aurora.Flowboard.Application.Authentication.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<IdentityToken>;
