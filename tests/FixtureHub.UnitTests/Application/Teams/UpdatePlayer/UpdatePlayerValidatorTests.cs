using FixtureHub.Application.Teams.UpdatePlayer;
using FluentValidation.TestHelper;

namespace FixtureHub.UnitTests.Application.Teams.UpdatePlayer;

public class UpdatePlayerValidatorTests
{
    private readonly UpdatePlayerValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var command = new UpdatePlayerCommand(Guid.NewGuid(), Guid.NewGuid(), "Jorge González", 10);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_BlankNameAndShirtNumberOutOfRange_ReturnsBothCodes()
    {
        var command = new UpdatePlayerCommand(Guid.NewGuid(), Guid.NewGuid(), " ", 0);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.Name).WithErrorCode("Player.NameRequired");
        result.ShouldHaveValidationErrorFor(command => command.ShirtNumber)
            .WithErrorCode("Player.InvalidShirtNumber");
    }
}
