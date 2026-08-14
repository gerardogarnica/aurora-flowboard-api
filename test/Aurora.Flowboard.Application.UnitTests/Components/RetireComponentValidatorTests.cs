namespace Aurora.Flowboard.Application.UnitTests.Components;

public sealed class RetireComponentValidatorTests
{
    private readonly RetireComponentValidator _validator;

    public RetireComponentValidatorTests()
    {
        _validator = new RetireComponentValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        RetireComponentCommand command = ComponentCommandData.GetRetireCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ComponentIdIsEmpty()
    {
        var command = new RetireComponentCommand(Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
