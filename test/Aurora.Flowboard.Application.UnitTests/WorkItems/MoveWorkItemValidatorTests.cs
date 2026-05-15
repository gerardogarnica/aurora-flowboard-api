namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class MoveWorkItemValidatorTests
{
    private readonly MoveWorkItemValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        MoveWorkItemCommand command = new(Guid.NewGuid(), Guid.NewGuid(), "Moving to next stage");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        MoveWorkItemCommand command = new(Guid.Empty, Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_ToStateIdIsEmpty()
    {
        MoveWorkItemCommand command = new(Guid.NewGuid(), Guid.Empty, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_ReasonExceedsMaxLength()
    {
        string longReason = new('A', StateTransitionHistory.MaxReasonLength + 1);
        MoveWorkItemCommand command = new(Guid.NewGuid(), Guid.NewGuid(), longReason);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_ReasonIsNull()
    {
        MoveWorkItemCommand command = new(Guid.NewGuid(), Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
