using FixtureHub.Domain.Teams;
using FluentValidation;

namespace FixtureHub.Application.Teams.UpdateTeam;

internal sealed class UpdateTeamValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithErrorCode(TeamErrors.NameRequired.Code)
            .MaximumLength(Team.NameMaxLength).WithErrorCode(TeamErrors.NameTooLong.Code);

        RuleFor(command => command.Country)
            .NotEmpty().WithErrorCode(TeamErrors.CountryRequired.Code)
            .MaximumLength(Team.CountryMaxLength).WithErrorCode(TeamErrors.CountryTooLong.Code);
    }
}
