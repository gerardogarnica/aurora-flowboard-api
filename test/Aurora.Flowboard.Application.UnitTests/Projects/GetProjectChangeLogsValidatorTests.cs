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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_BeInvalid_When_PageIsNotPositive(int page)
    {
        ValidationResult result = _validator.Validate(
            new GetProjectChangeLogsQuery(Guid.NewGuid(), page, ValidPageSize));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(PaginationDefaults.MaxPageSize + 1)]
    public void Should_BeInvalid_When_PageSizeIsOutOfRange(int pageSize)
    {
        ValidationResult result = _validator.Validate(
            new GetProjectChangeLogsQuery(Guid.NewGuid(), ValidPage, pageSize));

        result.IsValid.Should().BeFalse();
    }
}
