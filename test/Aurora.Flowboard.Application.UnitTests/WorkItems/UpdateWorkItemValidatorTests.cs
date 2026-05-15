namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class UpdateWorkItemValidatorTests
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly UpdateWorkItemValidator _validator;

    public UpdateWorkItemValidatorTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.Today.Returns(WorkItemCommandData.Today);
        _validator = new UpdateWorkItemValidator(_dateTimeProvider);
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        UpdateWorkItemCommand command = new(Guid.NewGuid(), WorkItemCommandData.UpdatedTitle, "A valid description", Priority.High, 5, WorkItemCommandData.EstimatedCompletionDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_IdIsEmpty()
    {
        UpdateWorkItemCommand command = new(Guid.Empty, WorkItemCommandData.UpdatedTitle, null, Priority.Medium, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TitleIsEmpty()
    {
        UpdateWorkItemCommand command = new(Guid.NewGuid(), string.Empty, null, Priority.Medium, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TitleExceedsMaxLength()
    {
        string longTitle = new('A', WorkItem.MaxTitleLength + 1);
        UpdateWorkItemCommand command = new(Guid.NewGuid(), longTitle, null, Priority.Medium, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', WorkItem.MaxDescriptionLength + 1);
        UpdateWorkItemCommand command = new(Guid.NewGuid(), WorkItemCommandData.UpdatedTitle, longDescription, Priority.Medium, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_DescriptionIsNull()
    {
        UpdateWorkItemCommand command = new(Guid.NewGuid(), WorkItemCommandData.UpdatedTitle, null, Priority.Medium, null, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_EstimatedPointsIsZero()
    {
        UpdateWorkItemCommand command = new(Guid.NewGuid(), WorkItemCommandData.UpdatedTitle, null, Priority.Medium, 0, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_EstimatedPointsIsPositive()
    {
        UpdateWorkItemCommand command = new(Guid.NewGuid(), WorkItemCommandData.UpdatedTitle, null, Priority.Medium, 5, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_EstimatedCompletionDateIsInThePast()
    {
        DateOnly pastDate = WorkItemCommandData.Today.AddDays(-1);
        UpdateWorkItemCommand command = new(Guid.NewGuid(), WorkItemCommandData.UpdatedTitle, null, Priority.Medium, null, pastDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_EstimatedCompletionDateIsToday()
    {
        UpdateWorkItemCommand command = new(Guid.NewGuid(), WorkItemCommandData.UpdatedTitle, null, Priority.Medium, null, WorkItemCommandData.Today);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
