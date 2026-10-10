using FixtureHub.Application.Matches.CorrectMatchResult;
using FixtureHub.Domain.Matches;
using FluentValidation.TestHelper;

namespace FixtureHub.UnitTests.Application.Matches.CorrectMatchResult;

public class CorrectMatchResultValidatorTests
{
    private readonly CorrectMatchResultValidator _validator = new();

    [Fact]
    public void Validate_NoGoalsAndAReason_HasNoErrors()
    {
        var result = _validator.TestValidate(new CorrectMatchResultCommand(Guid.NewGuid(), [], "Error de planilla"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_BlankReason_ReturnsCorrectionReasonRequired(string? reason)
    {
        var result = _validator.TestValidate(new CorrectMatchResultCommand(Guid.NewGuid(), [], reason!));

        result.ShouldHaveValidationErrorFor(command => command.Reason).WithErrorCode("Match.CorrectionReasonRequired");
    }

    [Fact]
    public void Validate_ReasonTooLong_ReturnsCorrectionReasonTooLong()
    {
        var reason = new string('A', MatchResultCorrection.ReasonMaxLength + 1);

        var result = _validator.TestValidate(new CorrectMatchResultCommand(Guid.NewGuid(), [], reason));

        result.ShouldHaveValidationErrorFor(command => command.Reason).WithErrorCode("Match.CorrectionReasonTooLong");
    }

    [Fact]
    public void Validate_WithoutGoalsList_ReturnsGoalsRequired()
    {
        var result = _validator.TestValidate(new CorrectMatchResultCommand(Guid.NewGuid(), null!, "Error de planilla"));

        result.ShouldHaveValidationErrorFor(command => command.Goals).WithErrorCode("Match.GoalsRequired");
    }

    [Fact]
    public void Validate_InvalidGoal_ReturnsTheErrorWithThePositionOfTheGoal()
    {
        var goals = new[] { new CorrectedGoal(Guid.NewGuid(), 10, false), new CorrectedGoal(Guid.Empty, 0, false) };

        var result = _validator.TestValidate(new CorrectMatchResultCommand(Guid.NewGuid(), goals, "Error de planilla"));

        result.ShouldHaveValidationErrorFor("Goals[1].PlayerId").WithErrorCode("Goal.PlayerRequired");
        result.ShouldHaveValidationErrorFor("Goals[1].Minute").WithErrorCode("Goal.InvalidMinute");
        result.ShouldNotHaveValidationErrorFor("Goals[0].Minute");
    }
}
