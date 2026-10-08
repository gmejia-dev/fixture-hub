using FixtureHub.Domain.Common;

namespace FixtureHub.UnitTests.Domain.Common;

public class ErrorTests
{
    [Fact]
    public void Factories_AssignTheExpectedType()
    {
        Assert.Equal(ErrorType.Failure, Error.Failure("Code", "Message").Type);
        Assert.Equal(ErrorType.Validation, Error.Validation("Code", "Message").Type);
        Assert.Equal(ErrorType.NotFound, Error.NotFound("Code", "Message").Type);
        Assert.Equal(ErrorType.Conflict, Error.Conflict("Code", "Message").Type);
    }

    [Fact]
    public void Factory_WithMetadata_KeepsTheMetadata()
    {
        var metadata = new Dictionary<string, object?> { ["currentStatus"] = "Finished" };

        var error = Error.Conflict("Match.NotInProgress", "Message", metadata);

        Assert.NotNull(error.Metadata);
        Assert.Equal("Finished", error.Metadata["currentStatus"]);
    }

    [Fact]
    public void Errors_WithSameValues_AreEqual()
    {
        var first = Error.NotFound("Team.NotFound", "El equipo no existe.");
        var second = Error.NotFound("Team.NotFound", "El equipo no existe.");

        Assert.Equal(first, second);
    }
}
