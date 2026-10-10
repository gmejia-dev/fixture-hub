namespace FixtureHub.Application.Pagination;

public interface IPagedQuery
{
    int PageNumber { get; }

    int PageSize { get; }

    string? SortBy { get; }

    string? SortDirection { get; }
}
