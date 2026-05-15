namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class RemoveWorkItemTagValidatorTests
{
    private readonly RemoveWorkItemTagValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        RemoveWorkItemTagCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_WorkItemIdIsEmpty()
    {
        RemoveWorkItemTagCommand command = new(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TagIdIsEmpty()
    {
        RemoveWorkItemTagCommand command = new(Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
