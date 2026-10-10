using FixtureHub.Domain.Matches;
using FluentValidation;

namespace FixtureHub.Application.Matches.ScheduleMatch;

internal sealed class ScheduleMatchValidator : AbstractValidator<ScheduleMatchCommand>
{
    public ScheduleMatchValidator()
    {
        RuleFor(command => command.HomeTeamId).NotEmpty().WithErrorCode(MatchErrors.HomeTeamRequired.Code);

        RuleFor(command => command.AwayTeamId).NotEmpty().WithErrorCode(MatchErrors.AwayTeamRequired.Code);

        RuleFor(command => command.ScheduledAt).NotEmpty().WithErrorCode(MatchErrors.ScheduledAtRequired.Code);
    }
}
