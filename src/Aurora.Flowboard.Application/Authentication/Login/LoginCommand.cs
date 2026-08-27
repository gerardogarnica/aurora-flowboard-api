namespace Aurora.Flowboard.Application.Authentication.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<IdentityToken>
{
    public override string ToString() => $"{nameof(LoginCommand)} {{ Email = {Email}, Password: [REDACTED] }}";
}
