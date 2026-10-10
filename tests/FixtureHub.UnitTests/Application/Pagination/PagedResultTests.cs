using FixtureHub.Application.Pagination;

namespace FixtureHub.UnitTests.Application.Pagination;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(250, 100, 3)]
    public void TotalPages_RoundsUpTheRecordsDividedByThePageSize(int totalRecords, int pageSize, int expected)
    {
        var page = new PagedResult<string>([], 1, pageSize, totalRecords);

        Assert.Equal(expected, page.TotalPages);
    }
}
