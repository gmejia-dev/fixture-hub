using FixtureHub.Application.Matches.GetMatches;
using FluentValidation.TestHelper;

namespace FixtureHub.UnitTests.Application.Matches.GetMatches;

public class GetMatchesValidatorTests
{
    private static readonly DateTimeOffset Day = new(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);

    private readonly GetMatchesValidator _validator = new();

    [Theory]
    [InlineData("Scheduled")]
    [InlineData("finished")]
    [InlineData("INPROGRESS")]
    [InlineData(null)]
    public void Validate_KnownStatusInAnyCase_IsAllowed(string? status)
    {
        var result = _validator.TestValidate(Query() with { Status = status });

        result.ShouldNotHaveValidationErrorFor(query => query.Status);
    }

    [Theory]
    [InlineData("Jugando")]
    [InlineData("2")]
    [InlineData("Finished,Cancelled")]
    public void Validate_UnknownStatus_ReturnsInvalidStatus(string status)
    {
        var result = _validator.TestValidate(Query() with { Status = status });

        result.ShouldHaveValidationErrorFor(query => query.Status).WithErrorCode("Query.InvalidStatus");
    }

    [Fact]
    public void Validate_FromAfterTo_ReturnsInvalidDateRange()
    {
        var result = _validator.TestValidate(Query() with { From = Day.AddDays(1), To = Day });

        result.ShouldHaveValidationErrorFor(query => query.To).WithErrorCode("Query.InvalidDateRange");
    }

    [Fact]
    public void Validate_SameDayOrOnlyOneLimit_IsAllowed()
    {
        _validator.TestValidate(Query() with { From = Day, To = Day }).ShouldNotHaveAnyValidationErrors();
        _validator.TestValidate(Query() with { From = Day }).ShouldNotHaveAnyValidationErrors();
        _validator.TestValidate(Query() with { To = Day }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_SortByOfAnotherQuery_ReturnsInvalidSortBy()
    {
        var result = _validator.TestValidate(Query() with { SortBy = "name" });

        result.ShouldHaveValidationErrorFor(query => query.SortBy).WithErrorCode("Query.InvalidSortBy");
    }

    private static GetMatchesQuery Query() => new(1, 10, null, null, null, null, null, null);
}
