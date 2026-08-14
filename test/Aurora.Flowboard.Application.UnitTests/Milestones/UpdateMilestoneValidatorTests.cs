namespace Aurora.Flowboard.Application.UnitTests.Milestones;

public sealed class UpdateMilestoneValidatorTests
{
    private readonly UpdateMilestoneValidator _validator;

    public UpdateMilestoneValidatorTests()
    {
        _validator = new UpdateMilestoneValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateMilestoneCommand command = MilestoneCommandData.GetUpdateCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_OptionalFieldsAreNull()
    {
        var command = new UpdateMilestoneCommand(Guid.NewGuid(), MilestoneCommandData.UpdatedName, null, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_MilestoneIdIsEmpty()
    {
        var command = new UpdateMilestoneCommand(Guid.Empty, MilestoneCommandData.UpdatedName, null, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        var command = new UpdateMilestoneCommand(Guid.NewGuid(), string.Empty, null, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Milestone.MaxNameLength + 1);
        var command = new UpdateMilestoneCommand(Guid.NewGuid(), longName, null, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', Milestone.MaxDescriptionLength + 1);
        var command = new UpdateMilestoneCommand(Guid.NewGuid(), MilestoneCommandData.UpdatedName, longDescription, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TargetEndDateBeforeTargetStartDate()
    {
        var laterDate = new DateOnly(2026, 2, 15);
        var earlierDate = new DateOnly(2026, 1, 15);
        var command = new UpdateMilestoneCommand(Guid.NewGuid(), MilestoneCommandData.UpdatedName, null, laterDate, earlierDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
