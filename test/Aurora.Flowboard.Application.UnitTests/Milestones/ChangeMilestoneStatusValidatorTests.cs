namespace Aurora.Flowboard.Application.UnitTests.Milestones;

public sealed class ChangeMilestoneStatusValidatorTests
{
    private readonly ChangeMilestoneStatusValidator _validator;

    public ChangeMilestoneStatusValidatorTests()
    {
        _validator = new ChangeMilestoneStatusValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        ChangeMilestoneStatusCommand command = MilestoneCommandData.GetChangeStatusCommand(Guid.NewGuid(), MilestoneStatus.Active);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_MilestoneIdIsEmpty()
    {
        var command = new ChangeMilestoneStatusCommand(Guid.Empty, MilestoneStatus.Active);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NewStatusIsNotDefined()
    {
        var command = new ChangeMilestoneStatusCommand(Guid.NewGuid(), (MilestoneStatus)999);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
