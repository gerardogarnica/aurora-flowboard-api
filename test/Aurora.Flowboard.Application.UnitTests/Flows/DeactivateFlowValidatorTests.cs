namespace Aurora.Flowboard.Application.UnitTests.Flows;

public sealed class DeactivateFlowValidatorTests
{
    private readonly DeactivateFlowValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        DeactivateFlowCommand command = new(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        DeactivateFlowCommand command = new(Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
