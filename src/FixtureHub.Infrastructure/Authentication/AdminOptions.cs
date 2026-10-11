namespace FixtureHub.Infrastructure.Authentication;

internal sealed class AdminOptions
{
    public const string SectionName = "Admin";
    public const int PasswordMinLength = 12;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
