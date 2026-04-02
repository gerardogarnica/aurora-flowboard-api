namespace Aurora.Flowboard.Application.Abstractions.Authentication;

public interface ITokenProvider
{
    IdentityToken CreateToken(TokenRequest tokenRequest);
}
