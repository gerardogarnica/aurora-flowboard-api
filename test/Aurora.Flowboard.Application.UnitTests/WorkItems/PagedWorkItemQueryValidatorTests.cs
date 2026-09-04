using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.WorkItems.GetChangeLogs;
using Aurora.Flowboard.Application.WorkItems.GetComments;
using Aurora.Flowboard.Application.WorkItems.GetStateHistory;
using Aurora.Flowboard.Application.WorkItems.GetTimeEntries;
using FluentValidation.Results;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class PagedWorkItemQueryValidatorTests
{
    private const int ValidPage = PaginationDefaults.DefaultPage;
    private const int ValidPageSize = PaginationDefaults.DefaultPageSize;

    private readonly GetWorkItemCommentsValidator _commentsValidator = new();
    private readonly GetWorkItemChangeLogsValidator _changeLogsValidator = new();
    private readonly GetWorkItemStateHistoryValidator _stateHistoryValidator = new();
    private readonly GetWorkItemTimeEntriesValidator _timeEntriesValidator = new();

    [Fact]
    public void Should_BeValid_When_QueryIsWellFormed()
    {
        ValidationResult result = _commentsValidator.Validate(
            new GetWorkItemCommentsQuery(Guid.NewGuid(), ValidPage, ValidPageSize));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_BeInvalid_When_WorkItemIdIsEmpty()
    {
        ValidationResult result = _commentsValidator.Validate(
            new GetWorkItemCommentsQuery(Guid.Empty, ValidPage, ValidPageSize));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_BeInvalid_When_PageIsNotPositive(int page)
    {
        ValidationResult result = _commentsValidator.Validate(
            new GetWorkItemCommentsQuery(Guid.NewGuid(), page, ValidPageSize));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(PaginationDefaults.MaxPageSize + 1)]
    public void Should_BeInvalid_When_PageSizeIsOutOfRange(int pageSize)
    {
        ValidationResult result = _commentsValidator.Validate(
            new GetWorkItemCommentsQuery(Guid.NewGuid(), ValidPage, pageSize));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_AllowTheMaximumPageSize_When_PageSizeIsAtTheLimit()
    {
        ValidationResult result = _commentsValidator.Validate(
            new GetWorkItemCommentsQuery(Guid.NewGuid(), ValidPage, PaginationDefaults.MaxPageSize));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_ApplyTheSamePaginationRules_When_ValidatingTheOtherActivityQueries()
    {
        int oversizedPage = PaginationDefaults.MaxPageSize + 1;

        _changeLogsValidator
            .Validate(new GetWorkItemChangeLogsQuery(Guid.NewGuid(), ValidPage, oversizedPage))
            .IsValid.Should().BeFalse();
        _stateHistoryValidator
            .Validate(new GetWorkItemStateHistoryQuery(Guid.NewGuid(), 0, ValidPageSize))
            .IsValid.Should().BeFalse();
        _timeEntriesValidator
            .Validate(new GetWorkItemTimeEntriesQuery(Guid.Empty, ValidPage, ValidPageSize))
            .IsValid.Should().BeFalse();

        _changeLogsValidator
            .Validate(new GetWorkItemChangeLogsQuery(Guid.NewGuid(), ValidPage, ValidPageSize))
            .IsValid.Should().BeTrue();
        _stateHistoryValidator
            .Validate(new GetWorkItemStateHistoryQuery(Guid.NewGuid(), ValidPage, ValidPageSize))
            .IsValid.Should().BeTrue();
        _timeEntriesValidator
            .Validate(new GetWorkItemTimeEntriesQuery(Guid.NewGuid(), ValidPage, ValidPageSize))
            .IsValid.Should().BeTrue();
    }
}
