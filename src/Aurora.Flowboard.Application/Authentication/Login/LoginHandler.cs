using Aurora.Flowboard.Domain.Shared;

namespace Aurora.Flowboard.Application.Authentication.Login;

internal sealed class LoginHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<LoginCommand, IdentityToken>
{
    public async Task<Result<IdentityToken>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        Result<Email> emailResult = Email.Create(command.Email);
        if (!emailResult.IsSuccessful)
        {
            passwordHasher.VerifyDummy(command.Password);
            return Result.Fail<IdentityToken>(AuthenticationErrors.InvalidCredentials);
        }

        string emailValue = emailResult.Value.Value;

        User? user = await dbContext
            .Users
            .SingleOrDefaultAsync(u => u.Email.Value == emailValue, cancellationToken);

        if (user is null)
        {
            passwordHasher.VerifyDummy(command.Password);
            return Result.Fail<IdentityToken>(AuthenticationErrors.InvalidCredentials);
        }

        bool passwordValid = user.VerifyPassword(passwordHasher, command.Password);

        if (!user.IsActive || !passwordValid)
        {
            return Result.Fail<IdentityToken>(AuthenticationErrors.InvalidCredentials);
        }

        List<string> roles = [.. user.Roles.Select(r => r.Name)];

        IdentityToken identityToken = tokenProvider.CreateToken(new TokenRequest(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            roles));

        Result<UserToken> issueResult = user.IssueToken(
            identityToken.AccessToken,
            identityToken.RefreshToken,
            identityToken.AccessTokenExpiresOn.UtcDateTime,
            identityToken.RefreshTokenExpiresOn.UtcDateTime,
            dateTimeProvider.UtcNow);

        if (!issueResult.IsSuccessful)
        {
            return Result.Fail<IdentityToken>(issueResult.Error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return identityToken;
    }
}
