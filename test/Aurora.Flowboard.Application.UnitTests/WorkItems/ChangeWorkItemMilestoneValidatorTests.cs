namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class ChangeWorkItemMilestoneValidatorTests
{
    private readonly ChangeWorkItemMilestoneValidator _validator = new();

    [Fact]
    public void Should_Pass_When_MilestoneIdIsProvided()
    {
        ChangeWorkItemMilestoneCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_MilestoneIdIsNull()
    {
        ChangeWorkItemMilestoneCommand command = new(Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        ChangeWorkItemMilestoneCommand command = new(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
