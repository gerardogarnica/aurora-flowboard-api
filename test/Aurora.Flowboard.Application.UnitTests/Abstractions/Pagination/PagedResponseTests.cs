using System.Globalization;
using Aurora.Flowboard.Application.Abstractions.Pagination;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions.Pagination;

public sealed class PagedResponseTests
{
    [Fact]
    public void Should_KeepThePagingMetadata_When_MappingTheItems()
    {
        // Arrange
        var page = new PagedResponse<int>([1, 2], 2, 2, 5);

        // Act
        PagedResponse<string> mapped = page.Map(i => i.ToString(CultureInfo.InvariantCulture));

        // Assert
        mapped.Items.Should().Equal("1", "2");
        mapped.Page.Should().Be(2);
        mapped.PageSize.Should().Be(2);
        mapped.TotalCount.Should().Be(5);
        mapped.TotalPages.Should().Be(3);
    }
}
