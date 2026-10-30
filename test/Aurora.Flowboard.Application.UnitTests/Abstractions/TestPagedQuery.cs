using Aurora.Flowboard.Application.Abstractions.Pagination;

namespace Aurora.Flowboard.Application.UnitTests.Abstractions;

internal sealed record TestPagedQuery(int Page, int PageSize) : IPagedQuery;
