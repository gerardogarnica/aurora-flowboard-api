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

    [Fact]
    public void Should_RejectAnEmptyWorkItemId_When_ValidatingTheOtherActivityQueries()
    {
        _changeLogsValidator
            .Validate(new GetWorkItemChangeLogsQuery(Guid.Empty, ValidPage, ValidPageSize))
            .IsValid.Should().BeFalse();
        _stateHistoryValidator
            .Validate(new GetWorkItemStateHistoryQuery(Guid.Empty, ValidPage, ValidPageSize))
            .IsValid.Should().BeFalse();
        _timeEntriesValidator
            .Validate(new GetWorkItemTimeEntriesQuery(Guid.Empty, ValidPage, ValidPageSize))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_ApplyThePagingRules_When_ValidatingAnyActivityQuery()
    {
        int oversizedPage = PaginationDefaults.MaxPageSize + 1;

        _commentsValidator
            .Validate(new GetWorkItemCommentsQuery(Guid.NewGuid(), 0, ValidPageSize))
            .IsValid.Should().BeFalse();
        _changeLogsValidator
            .Validate(new GetWorkItemChangeLogsQuery(Guid.NewGuid(), ValidPage, oversizedPage))
            .IsValid.Should().BeFalse();
        _stateHistoryValidator
            .Validate(new GetWorkItemStateHistoryQuery(Guid.NewGuid(), 0, ValidPageSize))
            .IsValid.Should().BeFalse();
        _timeEntriesValidator
            .Validate(new GetWorkItemTimeEntriesQuery(Guid.NewGuid(), ValidPage, oversizedPage))
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
