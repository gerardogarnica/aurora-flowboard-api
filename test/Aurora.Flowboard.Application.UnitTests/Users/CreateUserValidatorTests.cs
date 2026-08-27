using Aurora.Flowboard.Application.Users.CreateUser;

namespace Aurora.Flowboard.Application.UnitTests.Users;

public sealed class CreateUserValidatorTests
{
    private readonly CreateUserValidator _validator;

    public CreateUserValidatorTests()
    {
        _validator = new CreateUserValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        CreateUserCommand command = CreateUserCommandData.GetCreateCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_RoleIsEmpty()
    {
        CreateUserCommand command = CreateUserCommandData.GetCreateCommand(role: string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
