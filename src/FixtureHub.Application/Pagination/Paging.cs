namespace FixtureHub.Application.Pagination;

public static class Paging
{
    public const int FirstPage = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public const string Ascending = "asc";
    public const string Descending = "desc";

    public static bool IsValidDirection(string direction) =>
        direction.Equals(Ascending, StringComparison.OrdinalIgnoreCase)
        || direction.Equals(Descending, StringComparison.OrdinalIgnoreCase);

    public static bool IsDescending(string? direction) =>
        Descending.Equals(direction, StringComparison.OrdinalIgnoreCase);
}
