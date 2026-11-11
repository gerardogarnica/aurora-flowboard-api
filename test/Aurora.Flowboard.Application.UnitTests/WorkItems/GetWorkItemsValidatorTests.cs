using Aurora.Flowboard.Application.Abstractions.Pagination;
using FluentValidation.Results;

namespace Aurora.Flowboard.Application.UnitTests.WorkItems;

public sealed class GetWorkItemsValidatorTests
{
    private readonly GetWorkItemsValidator _validator = new();

    [Fact]
    public void Should_Pass_When_NoFilterIsGiven()
    {
        // Act
        ValidationResult result = _validator.Validate(Search());

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_EveryFilterHasAnAcceptedValue()
    {
        // Act
        ValidationResult result = _validator.Validate(
            Search(assignee: "me", createdBy: "me", stage: "InFlight", stalled: "true", priority: "High,Critical", sort: "stageThenTimeInState"));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_PriorityListHasSpacesAroundCommas()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(priority: "High, Critical"));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_AssigneeIsNotMe()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(assignee: "someone-else"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Assignee));
    }

    [Fact]
    public void Should_Fail_When_CreatedByIsNotLowercaseMe()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(createdBy: "Me"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.CreatedBy));
    }

    [Fact]
    public void Should_Fail_When_StageIsNotAStage()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(stage: "Done"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Stage));
    }

    [Fact]
    public void Should_Fail_When_StageHasTheWrongCase()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(stage: "inflight"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Stage));
    }

    [Fact]
    public void Should_Fail_When_StalledIsNotTrue()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(stalled: "false"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Stalled));
    }

    [Fact]
    public void Should_Fail_When_PriorityListHasAnUnknownValue()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(priority: "High,Urgent"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Priority));
    }

    [Fact]
    public void Should_Fail_When_PriorityHasTheWrongCase()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(priority: "high"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Priority));
    }

    [Fact]
    public void Should_Fail_When_PriorityIsANumber()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(priority: "2"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Priority));
    }

    [Fact]
    public void Should_Fail_When_PriorityListHasAnEmptyEntry()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(priority: "High,,Low"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Priority));
    }

    [Fact]
    public void Should_Fail_When_SortIsUnknown()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(sort: "createdOn"));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Sort));
    }

    [Fact]
    public void Should_Fail_When_PageIsZero()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(page: 0));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.Page));
    }

    [Fact]
    public void Should_Fail_When_PageSizeExceedsMaxPageSize()
    {
        // Act
        ValidationResult result = _validator.Validate(Search(pageSize: PaginationDefaults.MaxPageSize + 1));

        // Assert
        AssertSingleError(result, nameof(GetWorkItemsQuery.PageSize));
    }

    private static GetWorkItemsQuery Search(
        string? assignee = null,
        string? createdBy = null,
        string? stage = null,
        string? stalled = null,
        string? priority = null,
        string? sort = null,
        int page = PaginationDefaults.DefaultPage,
        int pageSize = PaginationDefaults.DefaultPageSize) =>
        new(assignee, createdBy, stage, stalled, priority, sort, page, pageSize);

    private static void AssertSingleError(ValidationResult result, string propertyName)
    {
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.PropertyName.Should().Be(propertyName);
    }
}
