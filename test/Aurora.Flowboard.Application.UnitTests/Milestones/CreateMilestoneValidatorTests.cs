namespace Aurora.Flowboard.Application.UnitTests.Milestones;

public sealed class CreateMilestoneValidatorTests
{
    private readonly CreateMilestoneValidator _validator;

    public CreateMilestoneValidatorTests()
    {
        _validator = new CreateMilestoneValidator();
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        CreateMilestoneCommand command = MilestoneCommandData.GetCreateCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_OptionalFieldsAreNull()
    {
        var command = new CreateMilestoneCommand(Guid.NewGuid(), MilestoneCommandData.Name, null, MilestoneCommandData.ColorValue, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        var command = new CreateMilestoneCommand(Guid.Empty, MilestoneCommandData.Name, null, MilestoneCommandData.ColorValue, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        var command = new CreateMilestoneCommand(Guid.NewGuid(), string.Empty, null, MilestoneCommandData.ColorValue, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', Milestone.MaxNameLength + 1);
        var command = new CreateMilestoneCommand(Guid.NewGuid(), longName, null, MilestoneCommandData.ColorValue, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', Milestone.MaxDescriptionLength + 1);
        var command = new CreateMilestoneCommand(Guid.NewGuid(), MilestoneCommandData.Name, longDescription, MilestoneCommandData.ColorValue, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TargetEndDateBeforeTargetStartDate()
    {
        var laterDate = new DateOnly(2026, 2, 15);
        var earlierDate = new DateOnly(2026, 1, 15);
        var command = new CreateMilestoneCommand(Guid.NewGuid(), MilestoneCommandData.Name, null, MilestoneCommandData.ColorValue, laterDate, earlierDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
    [Fact]
    public void Should_Fail_When_ColorIsEmpty()
    {
        var command = new CreateMilestoneCommand(Guid.NewGuid(), MilestoneCommandData.Name, null, string.Empty, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_ColorExceedsMaxLength()
    {
        string longColor = new('A', Color.MaxLength + 1);
        var command = new CreateMilestoneCommand(Guid.NewGuid(), MilestoneCommandData.Name, null, longColor, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
