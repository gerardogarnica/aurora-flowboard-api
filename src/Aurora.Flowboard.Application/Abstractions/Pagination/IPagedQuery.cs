namespace Aurora.Flowboard.Application.Abstractions.Pagination;

public interface IPagedQuery
{
    int Page { get; }

    int PageSize { get; }
}
