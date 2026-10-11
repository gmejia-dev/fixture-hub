using Microsoft.EntityFrameworkCore;

namespace FixtureHub.IntegrationTests.Persistence;

[Collection(nameof(SqlServerCollection))]
public sealed class DbContextConfigurationTests(SqlServerFixture database)
{
    [Fact]
    public async Task Query_WithTakeButWithoutOrderBy_FailsInsteadOfOnlyWarning()
    {
        var exception = await Record.ExceptionAsync(() =>
            database.QueryAsync(context => context.Teams.Take(1).ToListAsync()));

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("RowLimitingOperationWithoutOrderByWarning", exception.Message, StringComparison.Ordinal);
    }
}
