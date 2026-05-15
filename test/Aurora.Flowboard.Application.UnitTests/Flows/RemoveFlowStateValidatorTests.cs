namespace Aurora.Flowboard.Application.UnitTests.Flows;

public sealed class RemoveFlowStateValidatorTests
{
    private readonly RemoveFlowStateValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        RemoveFlowStateCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_FlowIdIsEmpty()
    {
        RemoveFlowStateCommand command = new(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_StateIdIsEmpty()
    {
        RemoveFlowStateCommand command = new(Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
