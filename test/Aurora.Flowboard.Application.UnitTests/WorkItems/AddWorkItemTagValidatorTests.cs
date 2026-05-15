namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class AddWorkItemTagValidatorTests
{
    private readonly AddWorkItemTagValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        AddWorkItemTagCommand command = new(Guid.NewGuid(), WorkItemCommandData.TagName);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_WorkItemIdIsEmpty()
    {
        AddWorkItemTagCommand command = new(Guid.Empty, WorkItemCommandData.TagName);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameIsEmpty()
    {
        AddWorkItemTagCommand command = new(Guid.NewGuid(), string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        string longName = new('A', WorkItemTag.MaxNameLength + 1);
        AddWorkItemTagCommand command = new(Guid.NewGuid(), longName);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
