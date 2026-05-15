namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemCommentValidatorTests
{
    private readonly UpdateWorkItemCommentValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemCommentCommand command = new(Guid.NewGuid(), Guid.NewGuid(), WorkItemCommandData.UpdatedContent);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_WorkItemIdIsEmpty()
    {
        UpdateWorkItemCommentCommand command = new(Guid.Empty, Guid.NewGuid(), WorkItemCommandData.UpdatedContent);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CommentIdIsEmpty()
    {
        UpdateWorkItemCommentCommand command = new(Guid.NewGuid(), Guid.Empty, WorkItemCommandData.UpdatedContent);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_ContentIsEmpty()
    {
        UpdateWorkItemCommentCommand command = new(Guid.NewGuid(), Guid.NewGuid(), string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_ContentExceedsMaxLength()
    {
        string longContent = new('A', Comment.MaxContentLength + 1);
        UpdateWorkItemCommentCommand command = new(Guid.NewGuid(), Guid.NewGuid(), longContent);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
