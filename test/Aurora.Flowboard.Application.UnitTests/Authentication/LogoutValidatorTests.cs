using Aurora.Flowboard.Application.Authentication.Logout;

namespace Aurora.Flowboard.Application.UnitTests.Authentication;

public sealed class LogoutValidatorTests
{
    private readonly LogoutValidator _validator;

    public LogoutValidatorTests()
    {
        _validator = new LogoutValidator();
    }

    [Fact]
    public void Should_Pass_When_RefreshTokenIsValid()
    {
        var command = new LogoutCommand("a-valid-refresh-token");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_RefreshTokenIsEmpty()
    {
        var command = new LogoutCommand(string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_RefreshTokenIsWhitespace()
    {
        var command = new LogoutCommand("   ");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_RefreshTokenExceedsMaxLength()
    {
        string longToken = new('A', UserToken.MaxRefreshTokenLength + 1);
        var command = new LogoutCommand(longToken);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
