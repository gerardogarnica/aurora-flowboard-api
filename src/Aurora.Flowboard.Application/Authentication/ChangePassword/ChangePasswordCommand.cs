namespace Aurora.Flowboard.Application.Authentication.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand
{
    public override string ToString() => $"{nameof(ChangePasswordCommand)} {{ Redacted }}";
}
