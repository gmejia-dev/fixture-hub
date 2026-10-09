using FixtureHub.Application.Teams.UpdateTeam;
using FixtureHub.Domain.Teams;
using FluentValidation.TestHelper;

namespace FixtureHub.UnitTests.Application.Teams.UpdateTeam;

public class UpdateTeamValidatorTests
{
    private readonly UpdateTeamValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new UpdateTeamCommand(Guid.NewGuid(), "Los Halcones", "El Salvador"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_BlankNameAndCountry_ReturnsTheRequiredCodes()
    {
        var result = _validator.TestValidate(new UpdateTeamCommand(Guid.NewGuid(), " ", ""));

        result.ShouldHaveValidationErrorFor(command => command.Name).WithErrorCode("Team.NameRequired");
        result.ShouldHaveValidationErrorFor(command => command.Country).WithErrorCode("Team.CountryRequired");
    }

    [Fact]
    public void Validate_NameAndCountryTooLong_ReturnsTheTooLongCodes()
    {
        var command = new UpdateTeamCommand(
            Guid.NewGuid(),
            new string('A', Team.NameMaxLength + 1),
            new string('A', Team.CountryMaxLength + 1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.Name).WithErrorCode("Team.NameTooLong");
        result.ShouldHaveValidationErrorFor(command => command.Country).WithErrorCode("Team.CountryTooLong");
    }
}
