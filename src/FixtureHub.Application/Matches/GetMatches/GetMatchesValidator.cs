using FixtureHub.Application.Errors;
using FixtureHub.Application.Pagination;
using FixtureHub.Domain.Matches;
using FluentValidation;

namespace FixtureHub.Application.Matches.GetMatches;

internal sealed class GetMatchesValidator : PagedQueryValidator<GetMatchesQuery>
{
    public GetMatchesValidator()
        : base(GetMatchesQuery.SortFields)
    {
        RuleFor(query => query.Status)
            .Must(status => status is null || Enum.GetNames<MatchStatus>().Contains(status, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode(QueryErrors.InvalidStatus.Code);

        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From)
            .When(query => query.From is not null && query.To is not null)
            .WithErrorCode(QueryErrors.InvalidDateRange.Code);
    }
}
