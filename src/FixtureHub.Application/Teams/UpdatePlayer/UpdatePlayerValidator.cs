using FixtureHub.Domain.Teams;
using FluentValidation;

namespace FixtureHub.Application.Teams.UpdatePlayer;

internal sealed class UpdatePlayerValidator : AbstractValidator<UpdatePlayerCommand>
{
    public UpdatePlayerValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithErrorCode(PlayerErrors.NameRequired.Code)
            .MaximumLength(Player.NameMaxLength).WithErrorCode(PlayerErrors.NameTooLong.Code);

        RuleFor(command => command.ShirtNumber)
            .InclusiveBetween(Player.MinShirtNumber, Player.MaxShirtNumber)
            .WithErrorCode(PlayerErrors.InvalidShirtNumber.Code);
    }
}
