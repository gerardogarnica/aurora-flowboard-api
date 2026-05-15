namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class AddWorkItemCommentValidatorTests
{
    private readonly AddWorkItemCommentValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        AddWorkItemCommentCommand command = new(Guid.NewGuid(), WorkItemCommandData.Content);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_WorkItemIdIsEmpty()
    {
        AddWorkItemCommentCommand command = new(Guid.Empty, WorkItemCommandData.Content);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_ContentIsEmpty()
    {
        AddWorkItemCommentCommand command = new(Guid.NewGuid(), string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_ContentExceedsMaxLength()
    {
        string longContent = new('A', Comment.MaxContentLength + 1);
        AddWorkItemCommentCommand command = new(Guid.NewGuid(), longContent);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
