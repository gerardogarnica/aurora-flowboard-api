namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class ChangeWorkItemComponentValidatorTests
{
    private readonly ChangeWorkItemComponentValidator _validator = new();

    [Fact]
    public void Should_Pass_When_ComponentIdIsProvided()
    {
        ChangeWorkItemComponentCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_ComponentIdIsNull()
    {
        ChangeWorkItemComponentCommand command = new(Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        ChangeWorkItemComponentCommand command = new(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
