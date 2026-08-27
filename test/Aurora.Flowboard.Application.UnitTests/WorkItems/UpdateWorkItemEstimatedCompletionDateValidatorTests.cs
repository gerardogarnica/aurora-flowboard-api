namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemEstimatedCompletionDateValidatorTests
{
    private readonly UpdateWorkItemEstimatedCompletionDateValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemEstimatedCompletionDateCommand command =
            new(Guid.NewGuid(), WorkItemCommandData.EstimatedCompletionDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_DateIsNull()
    {
        UpdateWorkItemEstimatedCompletionDateCommand command = new(Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemEstimatedCompletionDateCommand command =
            new(Guid.Empty, WorkItemCommandData.EstimatedCompletionDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
