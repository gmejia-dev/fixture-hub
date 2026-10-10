using FixtureHub.Application.Standings.GetStandings;
using FixtureHub.Application.Teams.GetPlayers;
using FixtureHub.Application.Teams.GetTeams;
using FixtureHub.Application.TopScorers.GetTopScorers;
using FluentValidation.TestHelper;

namespace FixtureHub.UnitTests.Application.Pagination;

public class PagedQueryValidatorTests
{
    private readonly GetTeamsValidator _validator = new();

    [Fact]
    public void Validate_FirstPageWithDefaults_HasNoErrors()
    {
        var result = _validator.TestValidate(Query());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageNumberBelowOne_ReturnsInvalidPageNumber(int pageNumber)
    {
        var result = _validator.TestValidate(Query() with { PageNumber = pageNumber });

        result.ShouldHaveValidationErrorFor(query => query.PageNumber).WithErrorCode("Query.InvalidPageNumber");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PageSizeOutsideOneTo100_ReturnsInvalidPageSize(int pageSize)
    {
        var result = _validator.TestValidate(Query() with { PageSize = pageSize });

        result.ShouldHaveValidationErrorFor(query => query.PageSize).WithErrorCode("Query.InvalidPageSize");
    }

    [Fact]
    public void Validate_PageSizeOf100_IsAllowed()
    {
        var result = _validator.TestValidate(Query() with { PageSize = 100 });

        result.ShouldNotHaveValidationErrorFor(query => query.PageSize);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("Name; DROP TABLE Teams--")]
    [InlineData("shirtNumber")]
    public void Validate_SortByOutsideTheWhitelist_ReturnsInvalidSortBy(string sortBy)
    {
        var result = _validator.TestValidate(Query() with { SortBy = sortBy });

        result.ShouldHaveValidationErrorFor(query => query.SortBy).WithErrorCode("Query.InvalidSortBy");
    }

    [Theory]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("playerCount")]
    [InlineData(null)]
    public void Validate_SortByInTheWhitelistInAnyCase_IsAllowed(string? sortBy)
    {
        var result = _validator.TestValidate(Query() with { SortBy = sortBy });

        result.ShouldNotHaveValidationErrorFor(query => query.SortBy);
    }

    [Theory]
    [InlineData("up")]
    [InlineData("descending")]
    public void Validate_UnknownSortDirection_ReturnsInvalidSortDirection(string direction)
    {
        var result = _validator.TestValidate(Query() with { SortDirection = direction });

        result.ShouldHaveValidationErrorFor(query => query.SortDirection).WithErrorCode("Query.InvalidSortDirection");
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("DESC")]
    [InlineData(null)]
    public void Validate_KnownSortDirectionInAnyCase_IsAllowed(string? direction)
    {
        var result = _validator.TestValidate(Query() with { SortDirection = direction });

        result.ShouldNotHaveValidationErrorFor(query => query.SortDirection);
    }

    [Fact]
    public void Validate_EachQuery_UsesItsOwnWhitelist()
    {
        new GetPlayersValidator()
            .TestValidate(new GetPlayersQuery(Guid.NewGuid(), 1, 10, "country", null, null))
            .ShouldHaveValidationErrorFor(query => query.SortBy).WithErrorCode("Query.InvalidSortBy");

        new GetStandingsValidator()
            .TestValidate(new GetStandingsQuery(1, 10, "goals", null, null))
            .ShouldHaveValidationErrorFor(query => query.SortBy).WithErrorCode("Query.InvalidSortBy");

        new GetTopScorersValidator()
            .TestValidate(new GetTopScorersQuery(1, 10, "points", null, null, null))
            .ShouldHaveValidationErrorFor(query => query.SortBy).WithErrorCode("Query.InvalidSortBy");
    }

    private static GetTeamsQuery Query() => new(1, 10, null, null, null, null);
}
