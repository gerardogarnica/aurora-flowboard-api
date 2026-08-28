namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemDescriptionValidatorTests
{
    private readonly UpdateWorkItemDescriptionValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemDescriptionCommand command = new(Guid.NewGuid(), "A new description");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_DescriptionIsNull()
    {
        UpdateWorkItemDescriptionCommand command = new(Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemDescriptionCommand command = new(Guid.Empty, "A new description");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', WorkItem.MaxDescriptionLength + 1);
        UpdateWorkItemDescriptionCommand command = new(Guid.NewGuid(), longDescription);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
