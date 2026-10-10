using FixtureHub.Application.Matches.RescheduleMatch;
using FluentValidation.TestHelper;

namespace FixtureHub.UnitTests.Application.Matches.RescheduleMatch;

public class RescheduleMatchValidatorTests
{
    private readonly RescheduleMatchValidator _validator = new();

    [Fact]
    public void Validate_WithDate_HasNoErrors()
    {
        var result = _validator.TestValidate(new RescheduleMatchCommand(Guid.NewGuid(), MatchScenario.KickOff));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithoutDate_ReturnsScheduledAtRequired()
    {
        var result = _validator.TestValidate(new RescheduleMatchCommand(Guid.NewGuid(), default));

        result.ShouldHaveValidationErrorFor(command => command.ScheduledAt).WithErrorCode("Match.ScheduledAtRequired");
    }
}
