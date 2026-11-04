using Aurora.Flowboard.Application.Abstractions.Pagination;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions.Pagination;

public sealed class QueryablePagingExtensionsTests
{
    private const int PageSize = 2;
    private const int TotalCount = 3;

    // Deliberately out of order, so the assertions prove the caller's ordering is applied before paging.
    private static readonly PagedItem[] Items = [new(3), new(1), new(2)];

    [Fact]
    public async Task Should_ReturnDifferentItemsPerPage_When_ResultsSpanSeveralPages()
    {
        // Act
        PagedResponse<int> firstPage = await ToPagedResponseAsync(Items, new TestPagedQuery(1, PageSize));
        PagedResponse<int> secondPage = await ToPagedResponseAsync(Items, new TestPagedQuery(2, PageSize));

        // Assert
        firstPage.Items.Should().Equal(1, 2);
        secondPage.Items.Should().Equal(3);
        firstPage.TotalCount.Should().Be(TotalCount);
        secondPage.TotalCount.Should().Be(TotalCount);
        secondPage.Page.Should().Be(2);
        secondPage.PageSize.Should().Be(PageSize);
    }

    [Fact]
    public async Task Should_ReturnEmptyItemsWithTheRealTotalCount_When_PageIsBeyondTheLastOne()
    {
        // Act
        PagedResponse<int> result = await ToPagedResponseAsync(Items, new TestPagedQuery(3, PageSize));

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(TotalCount);
        result.Page.Should().Be(3);
    }

    [Fact]
    public async Task Should_ReturnEmptyItems_When_SourceIsEmpty()
    {
        // Act
        PagedResponse<int> result = await ToPagedResponseAsync([], new TestPagedQuery(1, PageSize));

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(PageSize);
    }

    [Fact]
    public async Task Should_ReturnEmptyItemsWithoutOverflowing_When_PageIsTheLargestInt()
    {
        // Act
        PagedResponse<int> result = await ToPagedResponseAsync(
            Items,
            new TestPagedQuery(int.MaxValue, PaginationDefaults.MaxPageSize));

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(TotalCount);
        result.Page.Should().Be(int.MaxValue);
    }

    private static Task<PagedResponse<int>> ToPagedResponseAsync(PagedItem[] items, IPagedQuery paging)
    {
        IQueryable<PagedItem> source = new TestAsyncEnumerable<PagedItem>(items);

        return source
            .OrderBy(i => i.Number)
            .ToPagedResponseAsync(paging, i => i.Number, CancellationToken.None);
    }

    internal sealed record PagedItem(int Number);
}
