using FixtureHub.Application.Teams.CreateTeam;
using FixtureHub.Domain.Teams;
using FluentValidation.TestHelper;

namespace FixtureHub.UnitTests.Application.Teams.CreateTeam;

public class CreateTeamValidatorTests
{
    private readonly CreateTeamValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreateTeamCommand("Los Halcones", "El Salvador"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NameAndCountryAtMaximumLength_HasNoErrors()
    {
        var command = new CreateTeamCommand(
            new string('A', Team.NameMaxLength),
            new string('A', Team.CountryMaxLength));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_BlankNameAndCountry_ReturnsTheRequiredCodes(string? value)
    {
        var result = _validator.TestValidate(new CreateTeamCommand(value!, value!));

        result.ShouldHaveValidationErrorFor(command => command.Name).WithErrorCode("Team.NameRequired");
        result.ShouldHaveValidationErrorFor(command => command.Country).WithErrorCode("Team.CountryRequired");
    }

    [Fact]
    public void Validate_NameAndCountryTooLong_ReturnsTheTooLongCodes()
    {
        var command = new CreateTeamCommand(
            new string('A', Team.NameMaxLength + 1),
            new string('A', Team.CountryMaxLength + 1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.Name).WithErrorCode("Team.NameTooLong");
        result.ShouldHaveValidationErrorFor(command => command.Country).WithErrorCode("Team.CountryTooLong");
    }
}
