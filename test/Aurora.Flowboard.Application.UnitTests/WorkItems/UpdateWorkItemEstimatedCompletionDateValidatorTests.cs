namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemEstimatedCompletionDateValidatorTests
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly UpdateWorkItemEstimatedCompletionDateValidator _validator;

    public UpdateWorkItemEstimatedCompletionDateValidatorTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.Today.Returns(WorkItemCommandData.Today);
        _validator = new UpdateWorkItemEstimatedCompletionDateValidator(_dateTimeProvider);
    }

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

    [Fact]
    public void Should_Fail_When_DateIsInThePast()
    {
        DateOnly yesterday = WorkItemCommandData.Today.AddDays(-1);
        UpdateWorkItemEstimatedCompletionDateCommand command = new(Guid.NewGuid(), yesterday);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
