namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemTitleValidatorTests
{
    private readonly UpdateWorkItemTitleValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemTitleCommand command = new(Guid.NewGuid(), WorkItemCommandData.UpdatedTitle);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemTitleCommand command = new(Guid.Empty, WorkItemCommandData.UpdatedTitle);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TitleIsEmpty()
    {
        UpdateWorkItemTitleCommand command = new(Guid.NewGuid(), string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TitleExceedsMaxLength()
    {
        string longTitle = new('A', WorkItem.MaxTitleLength + 1);
        UpdateWorkItemTitleCommand command = new(Guid.NewGuid(), longTitle);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
