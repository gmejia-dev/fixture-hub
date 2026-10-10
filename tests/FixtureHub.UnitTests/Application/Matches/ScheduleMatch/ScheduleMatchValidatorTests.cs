using FixtureHub.Application.Matches.ScheduleMatch;
using FluentValidation.TestHelper;

namespace FixtureHub.UnitTests.Application.Matches.ScheduleMatch;

public class ScheduleMatchValidatorTests
{
    private readonly ScheduleMatchValidator _validator = new();

    [Fact]
    public void Validate_CompleteCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new ScheduleMatchCommand(Guid.NewGuid(), Guid.NewGuid(), MatchScenario.KickOff));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyCommand_ReturnsACodeForEachField()
    {
        var result = _validator.TestValidate(new ScheduleMatchCommand(Guid.Empty, Guid.Empty, default));

        result.ShouldHaveValidationErrorFor(command => command.HomeTeamId).WithErrorCode("Match.HomeTeamRequired");
        result.ShouldHaveValidationErrorFor(command => command.AwayTeamId).WithErrorCode("Match.AwayTeamRequired");
        result.ShouldHaveValidationErrorFor(command => command.ScheduledAt).WithErrorCode("Match.ScheduledAtRequired");
    }
}
