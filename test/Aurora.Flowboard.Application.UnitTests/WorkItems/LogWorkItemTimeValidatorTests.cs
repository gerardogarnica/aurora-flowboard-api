namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class LogWorkItemTimeValidatorTests
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly LogWorkItemTimeValidator _validator;

    public LogWorkItemTimeValidatorTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(WorkItemCommandData.UtcNow);
        _validator = new LogWorkItemTimeValidator(_dateTimeProvider);
    }

    [Fact]
    public void Should_Pass_When_CommandIsValid()
    {
        LogWorkItemTimeCommand command = new(Guid.NewGuid(), 2.5m, "Worked on feature", WorkItemCommandData.UtcNow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_WorkItemIdIsEmpty()
    {
        LogWorkItemTimeCommand command = new(Guid.Empty, 2.5m, null, WorkItemCommandData.UtcNow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_HoursIsZero()
    {
        LogWorkItemTimeCommand command = new(Guid.NewGuid(), 0m, null, WorkItemCommandData.UtcNow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_HoursIsNegative()
    {
        LogWorkItemTimeCommand command = new(Guid.NewGuid(), -1m, null, WorkItemCommandData.UtcNow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_DescriptionExceedsMaxLength()
    {
        string longDescription = new('A', TimeEntry.MaxDescriptionLength + 1);
        LogWorkItemTimeCommand command = new(Guid.NewGuid(), 2.5m, longDescription, WorkItemCommandData.UtcNow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_DescriptionIsNull()
    {
        LogWorkItemTimeCommand command = new(Guid.NewGuid(), 2.5m, null, WorkItemCommandData.UtcNow);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_LoggedOnUtcIsEmpty()
    {
        LogWorkItemTimeCommand command = new(Guid.NewGuid(), 2.5m, null, default);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_LoggedOnUtcIsInTheFuture()
    {
        DateTime futureDate = WorkItemCommandData.UtcNow.AddDays(1);
        LogWorkItemTimeCommand command = new(Guid.NewGuid(), 2.5m, null, futureDate);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
