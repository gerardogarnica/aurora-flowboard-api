using System.Linq.Expressions;

namespace Aurora.Flowboard.Application.Abstractions.Pagination;

internal static class QueryablePagingExtensions
{
    internal static async Task<PagedResponse<TResult>> ToPagedResponseAsync<TSource, TResult>(
        this IOrderedQueryable<TSource> source,
        IPagedQuery paging,
        Expression<Func<TSource, TResult>> selector,
        CancellationToken cancellationToken)
    {
        int totalCount = await source.CountAsync(cancellationToken);

        // In long because a huge page overflows int into a negative OFFSET, which Postgres rejects.
        // Any offset at or past the total count is a page beyond the last one, so it skips the items query.
        long offset = (long)(paging.Page - 1) * paging.PageSize;

        if (offset >= totalCount)
        {
            return new PagedResponse<TResult>([], paging.Page, paging.PageSize, totalCount);
        }

        List<TResult> items = await source
            .Skip((int)offset)
            .Take(paging.PageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return new PagedResponse<TResult>(items, paging.Page, paging.PageSize, totalCount);
    }
}
