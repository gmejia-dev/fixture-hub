using FixtureHub.Api.Http;
using FixtureHub.Domain.Common;

namespace FixtureHub.UnitTests.Api.Http;

public class ProblemMappingTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.Failure, 500)]
    public void StatusCodeFor_EachErrorType_ReturnsItsHttpStatus(ErrorType type, int expected)
    {
        Assert.Equal(expected, ProblemMapping.StatusCodeFor(type));
    }

    [Fact]
    public void ToProblem_CopiesTheMessageTheCodeAndTheMetadata()
    {
        var error = Error.Conflict("Player.ShirtNumberTaken", "El dorsal 10 ya está tomado.", new Dictionary<string, object?> { ["shirtNumber"] = 10 });

        var problem = error.ToProblem().ProblemDetails;

        Assert.Equal(409, problem.Status);
        Assert.Equal("El dorsal 10 ya está tomado.", problem.Detail);
        Assert.Equal("Player.ShirtNumberTaken", problem.Extensions["errorCode"]);
        var metadata = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(problem.Extensions["metadata"]);
        Assert.Equal(10, metadata["shirtNumber"]);
    }

    [Fact]
    public void ToProblem_ErrorWithoutMetadata_UsesAnEmptyDictionary()
    {
        var problem = Error.NotFound("Team.NotFound", "El equipo no existe.").ToProblem().ProblemDetails;

        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(problem.Extensions["metadata"]));
    }
}
