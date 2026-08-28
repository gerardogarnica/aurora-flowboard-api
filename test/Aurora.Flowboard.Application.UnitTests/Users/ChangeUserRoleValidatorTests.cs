using Aurora.Flowboard.Application.Users.ChangeRole;

namespace Aurora.Flowboard.Application.UnitTests.Users;

public sealed class ChangeUserRoleValidatorTests
{
    private readonly ChangeUserRoleValidator _validator;

    public ChangeUserRoleValidatorTests()
    {
        _validator = new ChangeUserRoleValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        var command = new ChangeUserRoleCommand(Guid.NewGuid(), Role.Member.Name);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_UserIdIsEmpty()
    {
        var command = new ChangeUserRoleCommand(Guid.Empty, Role.Member.Name);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_RoleIsEmpty()
    {
        var command = new ChangeUserRoleCommand(Guid.NewGuid(), string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
