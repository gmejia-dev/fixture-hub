namespace FixtureHub.Application.Authentication.Login;

public sealed record AuthToken(string AccessToken, string TokenType, DateTimeOffset ExpiresAt)
{
    public const string BearerType = "Bearer";
}
