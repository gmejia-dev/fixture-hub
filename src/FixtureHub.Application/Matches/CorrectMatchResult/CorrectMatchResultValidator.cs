using FixtureHub.Domain.Matches;
using FluentValidation;

namespace FixtureHub.Application.Matches.CorrectMatchResult;

internal sealed class CorrectMatchResultValidator : AbstractValidator<CorrectMatchResultCommand>
{
    public CorrectMatchResultValidator()
    {
        RuleFor(command => command.Reason)
            .NotEmpty().WithErrorCode(MatchErrors.CorrectionReasonRequired.Code)
            .MaximumLength(MatchResultCorrection.ReasonMaxLength).WithErrorCode(MatchErrors.CorrectionReasonTooLong.Code);

        RuleFor(command => command.Goals).NotNull().WithErrorCode(MatchErrors.GoalsRequired.Code);

        RuleForEach(command => command.Goals).ChildRules(goal =>
        {
            goal.RuleFor(g => g.PlayerId).NotEmpty().WithErrorCode(GoalErrors.PlayerRequired.Code);

            goal.RuleFor(g => g.Minute)
                .InclusiveBetween(Goal.MinMinute, Goal.MaxMinute)
                .WithErrorCode(GoalErrors.InvalidMinute.Code);
        });
    }
}
