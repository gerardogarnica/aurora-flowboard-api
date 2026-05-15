namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class CreateWorkItemValidatorTests
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CreateWorkItemValidator _validator;

    public CreateWorkItemValidatorTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.Today.Returns(WorkItemCommandData.Today);
        _validator = new CreateWorkItemValidator(_dateTimeProvider);
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        CreateWorkItemCommand command = new(
            WorkItemCommandData.Title,
            "A valid description",
            WorkItemType.Story,
            Priority.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            WorkItemCommandData.EstimatedCompletionDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_TitleIsEmpty()
    {
        CreateWorkItemCommand command = new(
            string.Empty,
            null,
            WorkItemType.Story,
            Priority.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_TitleExceedsMaxLength()
    {
        string longTitle = new('A', WorkItem.MaxTitleLength + 1);
        CreateWorkItemCommand command = new(
            longTitle,
            null,
            WorkItemType.Story,
            Priority.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', WorkItem.MaxDescriptionLength + 1);
        CreateWorkItemCommand command = new(
            WorkItemCommandData.Title,
            longDescription,
            WorkItemType.Story,
            Priority.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_DescriptionIsNull()
    {
        CreateWorkItemCommand command = new(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ProjectIdIsEmpty()
    {
        CreateWorkItemCommand command = new(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            Guid.Empty,
            Guid.NewGuid(),
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_FlowIdIsEmpty()
    {
        CreateWorkItemCommand command = new(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            Guid.NewGuid(),
            Guid.Empty,
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_EstimatedPointsIsZero()
    {
        CreateWorkItemCommand command = new(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_EstimatedPointsIsPositive()
    {
        CreateWorkItemCommand command = new(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            5,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_EstimatedCompletionDateIsInThePast()
    {
        DateOnly pastDate = WorkItemCommandData.Today.AddDays(-1);
        CreateWorkItemCommand command = new(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            pastDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_EstimatedCompletionDateIsToday()
    {
        CreateWorkItemCommand command = new(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            WorkItemCommandData.Today);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_AssigneeIdIsNotProvided()
    {
        CreateWorkItemCommand command = new(
            WorkItemCommandData.Title,
            null,
            WorkItemType.Story,
            Priority.Medium,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
