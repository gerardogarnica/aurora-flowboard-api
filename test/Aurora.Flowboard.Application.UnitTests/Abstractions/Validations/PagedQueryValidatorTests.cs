using Aurora.Flowboard.Application.Abstractions.Pagination;
using Aurora.Flowboard.Application.Abstractions.Validations;
using FluentValidation.Results;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions.Validations;

public sealed class PagedQueryValidatorTests
{
    private const int ValidPage = PaginationDefaults.DefaultPage;
    private const int ValidPageSize = PaginationDefaults.DefaultPageSize;

    private readonly PagedQueryValidator _validator = new();

    [Fact]
    public void Should_BeValid_When_PagingIsWellFormed()
    {
        ValidationResult result = _validator.Validate(new TestPagedQuery(ValidPage, ValidPageSize));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_BeInvalid_When_PageIsNotPositive(int page)
    {
        ValidationResult result = _validator.Validate(new TestPagedQuery(page, ValidPageSize));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(PaginationDefaults.MaxPageSize + 1)]
    public void Should_BeInvalid_When_PageSizeIsOutOfRange(int pageSize)
    {
        ValidationResult result = _validator.Validate(new TestPagedQuery(ValidPage, pageSize));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_AllowTheMaximumPageSize_When_PageSizeIsAtTheLimit()
    {
        ValidationResult result = _validator.Validate(new TestPagedQuery(ValidPage, PaginationDefaults.MaxPageSize));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_AllowTheLargestPage_When_PageIsPositive()
    {
        ValidationResult result = _validator.Validate(new TestPagedQuery(int.MaxValue, ValidPageSize));

        result.IsValid.Should().BeTrue();
    }
}
