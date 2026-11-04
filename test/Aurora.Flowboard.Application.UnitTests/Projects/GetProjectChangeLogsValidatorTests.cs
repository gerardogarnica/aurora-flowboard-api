using Aurora.Flowboard.Application.Abstractions.Pagination;
using FluentValidation.Results;

namespace Aurora.Flowboard.Application.UnitTests.Projects;

public sealed class GetProjectChangeLogsValidatorTests
{
    private const int ValidPage = PaginationDefaults.DefaultPage;
    private const int ValidPageSize = PaginationDefaults.DefaultPageSize;

    private readonly GetProjectChangeLogsValidator _validator = new();

    [Fact]
    public void Should_BeValid_When_QueryIsWellFormed()
    {
        ValidationResult result = _validator.Validate(
            new GetProjectChangeLogsQuery(Guid.NewGuid(), ValidPage, ValidPageSize));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_BeInvalid_When_ProjectIdIsEmpty()
    {
        ValidationResult result = _validator.Validate(
            new GetProjectChangeLogsQuery(Guid.Empty, ValidPage, ValidPageSize));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_ApplyThePagingRules_When_PagingIsOutOfRange()
    {
        _validator
            .Validate(new GetProjectChangeLogsQuery(Guid.NewGuid(), 0, ValidPageSize))
            .IsValid.Should().BeFalse();
        _validator
            .Validate(new GetProjectChangeLogsQuery(Guid.NewGuid(), ValidPage, PaginationDefaults.MaxPageSize + 1))
            .IsValid.Should().BeFalse();
    }
}
