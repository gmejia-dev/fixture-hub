using FixtureHub.Application.Teams.AddPlayer;
using FixtureHub.Domain.Teams;
using FluentValidation.TestHelper;

namespace FixtureHub.UnitTests.Application.Teams.AddPlayer;

public class AddPlayerValidatorTests
{
    private readonly AddPlayerValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void Validate_ShirtNumberWithinTheRange_HasNoErrors(int shirtNumber)
    {
        var result = _validator.TestValidate(new AddPlayerCommand(Guid.NewGuid(), "Jorge González", shirtNumber));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Validate_ShirtNumberOutOfRange_ReturnsInvalidShirtNumber(int shirtNumber)
    {
        var result = _validator.TestValidate(new AddPlayerCommand(Guid.NewGuid(), "Jorge González", shirtNumber));

        result.ShouldHaveValidationErrorFor(command => command.ShirtNumber)
            .WithErrorCode("Player.InvalidShirtNumber");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_BlankName_ReturnsNameRequired(string? name)
    {
        var result = _validator.TestValidate(new AddPlayerCommand(Guid.NewGuid(), name!, 10));

        result.ShouldHaveValidationErrorFor(command => command.Name).WithErrorCode("Player.NameRequired");
    }

    [Fact]
    public void Validate_NameTooLong_ReturnsNameTooLong()
    {
        var command = new AddPlayerCommand(Guid.NewGuid(), new string('A', Player.NameMaxLength + 1), 10);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(command => command.Name).WithErrorCode("Player.NameTooLong");
    }
}
