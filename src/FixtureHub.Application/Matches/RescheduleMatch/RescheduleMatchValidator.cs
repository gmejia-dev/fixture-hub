using FixtureHub.Domain.Matches;
using FluentValidation;

namespace FixtureHub.Application.Matches.RescheduleMatch;

internal sealed class RescheduleMatchValidator : AbstractValidator<RescheduleMatchCommand>
{
    public RescheduleMatchValidator() =>
        RuleFor(command => command.ScheduledAt).NotEmpty().WithErrorCode(MatchErrors.ScheduledAtRequired.Code);
}
