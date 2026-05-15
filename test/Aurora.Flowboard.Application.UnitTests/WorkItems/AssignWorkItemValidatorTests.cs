namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class AssignWorkItemValidatorTests
{
    private readonly AssignWorkItemValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        AssignWorkItemCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        AssignWorkItemCommand command = new(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_AssigneeIdIsEmpty()
    {
        AssignWorkItemCommand command = new(Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
