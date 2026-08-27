using Aurora.Flowboard.Application.Authentication.RefreshToken;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

public sealed class RefreshTokenValidatorTests
{
    private readonly RefreshTokenValidator _validator;

    public RefreshTokenValidatorTests()
    {
        _validator = new RefreshTokenValidator();
    }

    [Fact]
    public void Should_Pass_When_RefreshTokenIsValid()
    {
        var command = new RefreshTokenCommand("a-valid-refresh-token");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_RefreshTokenIsEmpty()
    {
        var command = new RefreshTokenCommand(string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_RefreshTokenIsWhitespace()
    {
        var command = new RefreshTokenCommand("   ");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_RefreshTokenExceedsMaxLength()
    {
        string longToken = new('A', UserToken.MaxRefreshTokenLength + 1);
        var command = new RefreshTokenCommand(longToken);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
