namespace FixtureHub.Application.Pagination;

public sealed record PagedResult<TItem>(IReadOnlyList<TItem> Data, int PageNumber, int PageSize, int TotalRecords)
{
    public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
}
