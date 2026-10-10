using FixtureHub.Application.Errors;
using FluentValidation;

namespace FixtureHub.Application.Pagination;

internal abstract class PagedQueryValidator<TQuery> : AbstractValidator<TQuery>
    where TQuery : IPagedQuery
{
    protected PagedQueryValidator(IReadOnlyCollection<string> sortFields)
    {
        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(Paging.FirstPage)
            .WithErrorCode(QueryErrors.InvalidPageNumber.Code);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, Paging.MaxPageSize)
            .WithErrorCode(QueryErrors.InvalidPageSize.Code);

        RuleFor(query => query.SortBy)
            .Must(sortBy => sortBy is null || sortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithErrorCode(QueryErrors.InvalidSortBy.Code);

        RuleFor(query => query.SortDirection)
            .Must(direction => direction is null || Paging.IsValidDirection(direction))
            .WithErrorCode(QueryErrors.InvalidSortDirection.Code);
    }
}
