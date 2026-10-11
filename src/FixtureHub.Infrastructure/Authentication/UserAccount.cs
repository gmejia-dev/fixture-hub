namespace FixtureHub.Infrastructure.Authentication;

internal sealed class UserAccount
{
    public const int UsernameMaxLength = 50;
    public const int PasswordHashMaxLength = 200;
    public const int RoleMaxLength = 20;

    public UserAccount(Guid id, string username, string passwordHash, string role)
    {
        Id = id;
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
    }

    public Guid Id { get; private init; }

    public string Username { get; private init; }

    public string PasswordHash { get; private set; }

    public string Role { get; private init; }

    public void ChangePasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
