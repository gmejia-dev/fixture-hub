using System.Data.Common;
using Dapper;
using FixtureHub.Application.Pagination;

namespace FixtureHub.Infrastructure.Reading;

internal sealed class PagedSql(
    string source,
    string columns,
    IReadOnlyDictionary<string, string> sortColumns,
    string defaultSortBy,
    string tieBreaker)
{
    private readonly List<string> _conditions = [];
    private readonly DynamicParameters _parameters = new();

    public PagedSql Parameter(string name, object? value)
    {
        _parameters.Add(name, value);

        return this;
    }

    public PagedSql Where(string condition, string parameter, object? value)
    {
        _conditions.Add(condition);

        return Parameter(parameter, value);
    }

    public async Task<PagedResult<TItem>> QueryAsync<TItem>(
        DbConnection connection,
        IPagedQuery query,
        CancellationToken cancellationToken)
    {
        _parameters.Add("Offset", (long)(query.PageNumber - 1) * query.PageSize);
        _parameters.Add("PageSize", query.PageSize);

        var command = new CommandDefinition(ToSql(query), _parameters, cancellationToken: cancellationToken);
        await using var results = await connection.QueryMultipleAsync(command);

        var totalRecords = await results.ReadSingleAsync<int>();
        var items = (await results.ReadAsync<TItem>()).AsList();

        return new PagedResult<TItem>(items, query.PageNumber, query.PageSize, totalRecords);
    }

    public string ToSql(IPagedQuery query)
    {
        var where = _conditions.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", _conditions)}";

        return $"""
            WITH [Rows] AS ({source})
            SELECT COUNT(*) FROM [Rows] {where};

            WITH [Rows] AS ({source})
            SELECT {columns} FROM [Rows] {where}
            ORDER BY {OrderBy(query)}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;
    }

    private string OrderBy(IPagedQuery query)
    {
        var sortBy = query.SortBy ?? defaultSortBy;
        if (!sortColumns.TryGetValue(sortBy, out var column))
            throw new InvalidOperationException($"El campo de ordenamiento '{sortBy}' no está en la lista blanca.");

        var direction = Paging.IsDescending(query.SortDirection) ? "DESC" : "ASC";

        return $"{column} {direction}, {tieBreaker}";
    }
}
