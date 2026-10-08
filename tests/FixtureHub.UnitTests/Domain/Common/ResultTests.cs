using FixtureHub.Domain.Common;

namespace FixtureHub.UnitTests.Domain.Common;

public class ResultTests
{
    private static readonly Error SampleError =
        Error.Conflict("Match.NotInProgress", "Solo se pueden registrar goles en un partido en curso.");

    [Fact]
    public void Success_IsSuccessfulAndHasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_IsFailedAndKeepsTheError()
    {
        var result = Result.Failure(SampleError);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Same(SampleError, result.Error);
    }

    [Fact]
    public void Failure_WithErrorNone_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void ImplicitConversion_FromError_CreatesFailure()
    {
        Result result = SampleError;

        Assert.True(result.IsFailure);
        Assert.Same(SampleError, result.Error);
    }

    [Fact]
    public void GenericSuccess_ExposesTheValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_AccessingValue_ThrowsInvalidOperationException()
    {
        var result = Result.Failure<int>(SampleError);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void GenericImplicitConversion_FromValue_CreatesSuccess()
    {
        Result<string> result = "FixtureHub";

        Assert.True(result.IsSuccess);
        Assert.Equal("FixtureHub", result.Value);
    }

    [Fact]
    public void GenericImplicitConversion_FromError_CreatesFailure()
    {
        Result<string> result = SampleError;

        Assert.True(result.IsFailure);
        Assert.Same(SampleError, result.Error);
    }

    [Fact]
    public void GenericResult_CanBeHandledAsNonGenericResult()
    {
        Result result = Result.Success(42);

        Assert.True(result.IsSuccess);
    }
}
