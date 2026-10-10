using FixtureHub.Domain.Matches;
using FluentValidation;

namespace FixtureHub.Application.Matches.RegisterGoal;

internal sealed class RegisterGoalValidator : AbstractValidator<RegisterGoalCommand>
{
    public RegisterGoalValidator()
    {
        RuleFor(command => command.PlayerId).NotEmpty().WithErrorCode(GoalErrors.PlayerRequired.Code);

        RuleFor(command => command.Minute)
            .InclusiveBetween(Goal.MinMinute, Goal.MaxMinute)
            .WithErrorCode(GoalErrors.InvalidMinute.Code);
    }
}
