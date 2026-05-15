namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UnassignWorkItemValidatorTests
{
    private readonly UnassignWorkItemValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UnassignWorkItemCommand command = new(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UnassignWorkItemCommand command = new(Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
