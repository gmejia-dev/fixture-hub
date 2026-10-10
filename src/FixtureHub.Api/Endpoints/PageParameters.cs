using FixtureHub.Application.Pagination;

namespace FixtureHub.Api.Endpoints;

internal sealed record PageParameters(int? PageNumber, int? PageSize, string? SortBy, string? SortDirection)
{
    public int Number => PageNumber ?? Paging.FirstPage;

    public int Size => PageSize ?? Paging.DefaultPageSize;
}
