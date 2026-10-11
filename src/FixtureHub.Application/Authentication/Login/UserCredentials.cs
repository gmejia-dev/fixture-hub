namespace FixtureHub.Application.Authentication.Login;

public sealed record UserCredentials(Guid UserId, string Username, string PasswordHash, string Role);
