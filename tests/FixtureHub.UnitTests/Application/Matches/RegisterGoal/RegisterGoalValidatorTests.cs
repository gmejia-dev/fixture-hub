using FixtureHub.Application.Matches.RegisterGoal;
using FluentValidation.TestHelper;

namespace FixtureHub.UnitTests.Application.Matches.RegisterGoal;

public class RegisterGoalValidatorTests
{
    private readonly RegisterGoalValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    public void Validate_MinuteWithinTheRange_HasNoErrors(int minute)
    {
        var result = _validator.TestValidate(new RegisterGoalCommand(Guid.NewGuid(), Guid.NewGuid(), minute, false));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public void Validate_MinuteOutOfRange_ReturnsInvalidMinute(int minute)
    {
        var result = _validator.TestValidate(new RegisterGoalCommand(Guid.NewGuid(), Guid.NewGuid(), minute, false));

        result.ShouldHaveValidationErrorFor(command => command.Minute).WithErrorCode("Goal.InvalidMinute");
    }

    [Fact]
    public void Validate_WithoutPlayer_ReturnsPlayerRequired()
    {
        var result = _validator.TestValidate(new RegisterGoalCommand(Guid.NewGuid(), Guid.Empty, 30, false));

        result.ShouldHaveValidationErrorFor(command => command.PlayerId).WithErrorCode("Goal.PlayerRequired");
    }
}
